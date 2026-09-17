package main

import (
	"bufio"
	"crypto/rand"
	"crypto/subtle"
	"encoding/hex"
	"encoding/json"
	"errors"
	"flag"
	"fmt"
	"io"
	"log"
	"net"
	"os"
	"strconv"
	"sync"
	"time"
)

const (
	maxHandshakeSize = 8 * 1024
	pairingTimeout   = 20 * time.Second
)

type message struct {
	Type         string `json:"type"`
	Secret       string `json:"secret,omitempty"`
	Name         string `json:"name,omitempty"`
	Token        string `json:"token,omitempty"`
	ConnectionID string `json:"connection_id,omitempty"`
	PublicHost   string `json:"public_host,omitempty"`
	PublicPort   int    `json:"public_port,omitempty"`
	Error        string `json:"error,omitempty"`
}

type server struct {
	secret     string
	publicHost string
	portStart  int
	portEnd    int

	mu       sync.Mutex
	sessions map[string]*session
	ports    map[int]bool
}

type session struct {
	server     *server
	token      string
	name       string
	publicPort int
	listener   net.Listener
	control    net.Conn
	encoder    *json.Encoder
	sendMu     sync.Mutex
	done       chan struct{}
	closeOnce  sync.Once

	mu      sync.Mutex
	pending map[string]*pendingConnection
}

type pendingConnection struct {
	player net.Conn
	data   chan net.Conn
}

type bufferedConn struct {
	net.Conn
	reader *bufio.Reader
}

func (c *bufferedConn) Read(p []byte) (int, error) {
	return c.reader.Read(p)
}

func main() {
	listen := flag.String("listen", ":7000", "control and data channel listen address")
	publicHost := flag.String("public-host", "", "public hostname or IP shown to clients")
	portStart := flag.Int("port-start", 30000, "first public tunnel port")
	portEnd := flag.Int("port-end", 30100, "last public tunnel port")
	secret := flag.String("secret", "", "shared relay secret")
	flag.Parse()

	resolvedSecret := *secret
	if resolvedSecret == "" {
		resolvedSecret = os.Getenv("MINETUNNEL_SECRET")
	}
	if resolvedSecret == "" {
		log.Fatal("--secret or MINETUNNEL_SECRET is required")
	}
	if *publicHost == "" {
		log.Fatal("--public-host is required")
	}
	if *portStart < 1 || *portEnd > 65535 || *portStart > *portEnd {
		log.Fatal("invalid public port range")
	}

	s := &server{
		secret:     resolvedSecret,
		publicHost: *publicHost,
		portStart:  *portStart,
		portEnd:    *portEnd,
		sessions:   make(map[string]*session),
		ports:      make(map[int]bool),
	}

	listener, err := net.Listen("tcp", *listen)
	if err != nil {
		log.Fatalf("listen on %s: %v", *listen, err)
	}
	log.Printf("MineTunnel relay listening on %s; public ports %d-%d", *listen, *portStart, *portEnd)

	for {
		conn, err := listener.Accept()
		if err != nil {
			log.Printf("accept control connection: %v", err)
			continue
		}
		go s.handleConnection(conn)
	}
}

func (s *server) handleConnection(conn net.Conn) {
	reader := bufio.NewReaderSize(conn, maxHandshakeSize)
	_ = conn.SetReadDeadline(time.Now().Add(10 * time.Second))
	line, err := reader.ReadSlice('\n')
	if err != nil || len(line) > maxHandshakeSize {
		conn.Close()
		return
	}
	_ = conn.SetReadDeadline(time.Time{})

	var msg message
	if err := json.Unmarshal(line, &msg); err != nil {
		conn.Close()
		return
	}

	switch msg.Type {
	case "register":
		s.handleRegister(conn, reader, msg)
		conn.Close()
	case "data":
		if !s.attachData(conn, reader, msg) {
			conn.Close()
		}
	default:
		_ = json.NewEncoder(conn).Encode(message{Type: "error", Error: "unknown handshake type"})
		conn.Close()
	}
}

func (s *server) handleRegister(conn net.Conn, reader *bufio.Reader, msg message) {
	if subtle.ConstantTimeCompare([]byte(msg.Secret), []byte(s.secret)) != 1 {
		_ = json.NewEncoder(conn).Encode(message{Type: "error", Error: "authentication failed"})
		return
	}

	listener, port, err := s.reservePort()
	if err != nil {
		_ = json.NewEncoder(conn).Encode(message{Type: "error", Error: err.Error()})
		return
	}

	token, err := randomID(24)
	if err != nil {
		listener.Close()
		s.releasePort(port)
		_ = json.NewEncoder(conn).Encode(message{Type: "error", Error: "could not create session"})
		return
	}

	sess := &session{
		server:     s,
		token:      token,
		name:       msg.Name,
		publicPort: port,
		listener:   listener,
		control:    conn,
		encoder:    json.NewEncoder(conn),
		done:       make(chan struct{}),
		pending:    make(map[string]*pendingConnection),
	}

	s.mu.Lock()
	s.sessions[token] = sess
	s.mu.Unlock()
	defer sess.close()

	if err := sess.send(message{
		Type:       "registered",
		Token:      token,
		PublicHost: s.publicHost,
		PublicPort: port,
	}); err != nil {
		return
	}

	log.Printf("tunnel registered name=%q public=%s:%d", msg.Name, s.publicHost, port)
	go sess.acceptPlayers()
	_, _ = io.Copy(io.Discard, reader)
}

func (s *server) reservePort() (net.Listener, int, error) {
	for port := s.portStart; port <= s.portEnd; port++ {
		s.mu.Lock()
		if s.ports[port] {
			s.mu.Unlock()
			continue
		}
		s.ports[port] = true
		s.mu.Unlock()

		listener, err := net.Listen("tcp", ":"+strconv.Itoa(port))
		if err == nil {
			return listener, port, nil
		}
		s.releasePort(port)
	}
	return nil, 0, errors.New("no public ports available")
}

func (s *server) releasePort(port int) {
	s.mu.Lock()
	delete(s.ports, port)
	s.mu.Unlock()
}

func (s *server) attachData(conn net.Conn, reader *bufio.Reader, msg message) bool {
	if msg.Token == "" || msg.ConnectionID == "" {
		return false
	}

	s.mu.Lock()
	sess := s.sessions[msg.Token]
	s.mu.Unlock()
	if sess == nil {
		return false
	}

	sess.mu.Lock()
	pending := sess.pending[msg.ConnectionID]
	if pending != nil {
		delete(sess.pending, msg.ConnectionID)
	}
	sess.mu.Unlock()
	if pending == nil {
		return false
	}

	wrapped := &bufferedConn{Conn: conn, reader: reader}
	select {
	case pending.data <- wrapped:
		return true
	case <-sess.done:
		return false
	}
}

func (s *session) acceptPlayers() {
	for {
		conn, err := s.listener.Accept()
		if err != nil {
			select {
			case <-s.done:
				return
			default:
				log.Printf("accept player on port %d: %v", s.publicPort, err)
				continue
			}
		}
		go s.handlePlayer(conn)
	}
}

func (s *session) handlePlayer(player net.Conn) {
	connectionID, err := randomID(16)
	if err != nil {
		player.Close()
		return
	}

	pending := &pendingConnection{player: player, data: make(chan net.Conn, 1)}
	s.mu.Lock()
	select {
	case <-s.done:
		s.mu.Unlock()
		player.Close()
		return
	default:
		s.pending[connectionID] = pending
	}
	s.mu.Unlock()
	defer s.removePending(connectionID, pending)

	if err := s.send(message{Type: "open", ConnectionID: connectionID}); err != nil {
		player.Close()
		s.close()
		return
	}

	timer := time.NewTimer(pairingTimeout)
	defer timer.Stop()
	select {
	case data := <-pending.data:
		log.Printf("player connected tunnel=%q remote=%s", s.name, player.RemoteAddr())
		bridge(player, data)
		log.Printf("player disconnected tunnel=%q remote=%s", s.name, player.RemoteAddr())
	case <-timer.C:
		player.Close()
		log.Printf("data channel timeout tunnel=%q", s.name)
	case <-s.done:
		player.Close()
	}
}

func (s *session) removePending(id string, pending *pendingConnection) {
	s.mu.Lock()
	if s.pending[id] == pending {
		delete(s.pending, id)
	}
	s.mu.Unlock()
}

func (s *session) send(msg message) error {
	s.sendMu.Lock()
	defer s.sendMu.Unlock()
	return s.encoder.Encode(msg)
}

func (s *session) close() {
	s.closeOnce.Do(func() {
		close(s.done)
		s.listener.Close()

		s.mu.Lock()
		for id, pending := range s.pending {
			pending.player.Close()
			delete(s.pending, id)
		}
		s.mu.Unlock()

		s.server.mu.Lock()
		delete(s.server.sessions, s.token)
		delete(s.server.ports, s.publicPort)
		s.server.mu.Unlock()
		log.Printf("tunnel closed name=%q public_port=%d", s.name, s.publicPort)
	})
}

func randomID(bytes int) (string, error) {
	buffer := make([]byte, bytes)
	if _, err := rand.Read(buffer); err != nil {
		return "", err
	}
	return hex.EncodeToString(buffer), nil
}

func bridge(a, b net.Conn) {
	done := make(chan struct{}, 2)
	copyOneWay := func(dst, src net.Conn) {
		_, _ = io.Copy(dst, src)
		done <- struct{}{}
	}
	go copyOneWay(a, b)
	go copyOneWay(b, a)
	<-done
	a.Close()
	b.Close()
	<-done
}

func (m message) String() string {
	return fmt.Sprintf("type=%s name=%s public_port=%d", m.Type, m.Name, m.PublicPort)
}
