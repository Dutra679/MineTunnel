# Protocolo MineTunnel v0.1

O protocolo usa TCP. Cada conexao com a porta de controle comeca com uma unica
mensagem JSON UTF-8 terminada por `\n`. O campo `type` define o papel do socket.

O handshake e limitado a 8 KiB e deve chegar em ate 10 segundos.

## Registro do tunel

Cliente para relay:

```json
{"type":"register","secret":"SEGREDO_COMPARTILHADO","name":"PC-do-anfitriao"}
```

Relay para cliente em caso de sucesso:

```json
{
  "type": "registered",
  "token": "TOKEN_ALEATORIO_DA_SESSAO",
  "public_host": "relay.exemplo.com",
  "public_port": 30000
}
```

O token possui 24 bytes aleatorios codificados em hexadecimal. Ele permanece
valido somente enquanto o canal de controle estiver conectado.

## Solicitacao de canal

Quando um jogador conecta na porta publica, o relay envia pelo controle:

```json
{"type":"open","connection_id":"ID_ALEATORIO_DA_CONEXAO"}
```

O identificador possui 16 bytes aleatorios codificados em hexadecimal e pode ser
usado apenas uma vez.

## Canal de dados

O aplicativo abre outro socket para a porta de controle e envia:

```json
{
  "type": "data",
  "token": "TOKEN_ALEATORIO_DA_SESSAO",
  "connection_id": "ID_ALEATORIO_DA_CONEXAO"
}
```

Quando token e identificador correspondem a uma pendencia, o relay conecta esse
socket ao jogador. A partir do byte seguinte ao `\n`, todo conteudo e tratado
como TCP bruto. Nao existe enquadramento adicional.

## Erros

```json
{"type":"error","error":"descricao"}
```

Erros conhecidos:

- `authentication failed`;
- `no public ports available`;
- `could not create session`;
- `unknown handshake type`.

Handshakes invalidos, expirados ou sem pareamento podem ser encerrados sem uma
mensagem de erro.

## Compatibilidade

O campo de versao ainda nao faz parte do handshake. Alteracoes incompativeis
devem introduzir negociacao explicita antes de modificar mensagens existentes.
Campos JSON desconhecidos devem ser ignorados por implementacoes compativeis.

## Consideracoes de seguranca

O segredo e o token trafegam sem criptografia. Um observador capaz de capturar o
trafego pode reutiliza-los durante a sessao. O protocolo atual deve ser usado
somente como MVP em ambientes controlados.

Uma evolucao segura deve adicionar:

1. TLS com verificacao de certificado no cliente;
2. credenciais individuais com expiracao e revogacao;
3. protecao contra repeticao;
4. limites de registros, jogadores e bytes por identidade;
5. versao negociada do protocolo.
