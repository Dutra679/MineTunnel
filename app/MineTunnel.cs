using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MineTunnelApp
{
    internal sealed class AppConfig
    {
        public string relay { get; set; }
        public string local { get; set; }
        public string secret { get; set; }
        public string name { get; set; }
    }

    internal sealed class ProtocolMessage
    {
        public string type { get; set; }
        public string secret { get; set; }
        public string name { get; set; }
        public string token { get; set; }
        public string connection_id { get; set; }
        public string public_host { get; set; }
        public int public_port { get; set; }
        public string error { get; set; }
    }

    internal enum TunnelState
    {
        Idle,
        Connecting,
        Active,
        Error
    }

    internal sealed class MainController
    {
        private const string Xaml = @"
<Window xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
        xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
        Title=""MineTunnel"" Width=""760"" Height=""680""
        MinWidth=""720"" MinHeight=""640"" WindowStartupLocation=""CenterScreen""
        WindowStyle=""None"" ResizeMode=""CanResizeWithGrip""
        Background=""#101412"" Foreground=""#F3F7F4""
        FontFamily=""Segoe UI"" TextOptions.TextFormattingMode=""Display"">
  <Window.Resources>
    <SolidColorBrush x:Key=""PanelBrush"" Color=""#18201C""/>
    <SolidColorBrush x:Key=""PanelHoverBrush"" Color=""#202A25""/>
    <SolidColorBrush x:Key=""BorderBrush"" Color=""#2B3731""/>
    <SolidColorBrush x:Key=""MutedBrush"" Color=""#92A098""/>
    <SolidColorBrush x:Key=""AccentBrush"" Color=""#43D17A""/>
    <SolidColorBrush x:Key=""AccentDarkBrush"" Color=""#173525""/>
    <Style TargetType=""TextBlock""><Setter Property=""TextWrapping"" Value=""Wrap""/></Style>
    <Style x:Key=""FieldLabel"" TargetType=""TextBlock"">
      <Setter Property=""Foreground"" Value=""#92A098""/><Setter Property=""FontSize"" Value=""11""/>
      <Setter Property=""FontWeight"" Value=""SemiBold""/>
    </Style>
    <Style x:Key=""InputStyle"" TargetType=""TextBox"">
      <Setter Property=""Height"" Value=""42""/><Setter Property=""Padding"" Value=""12,9""/>
      <Setter Property=""Background"" Value=""#111714""/><Setter Property=""Foreground"" Value=""#F3F7F4""/>
      <Setter Property=""BorderBrush"" Value=""#35433C""/><Setter Property=""BorderThickness"" Value=""1""/>
      <Setter Property=""FontSize"" Value=""14""/><Setter Property=""CaretBrush"" Value=""#43D17A""/>
    </Style>
    <Style x:Key=""PasswordStyle"" TargetType=""PasswordBox"">
      <Setter Property=""Height"" Value=""42""/><Setter Property=""Padding"" Value=""12,9""/>
      <Setter Property=""Background"" Value=""#111714""/><Setter Property=""Foreground"" Value=""#F3F7F4""/>
      <Setter Property=""BorderBrush"" Value=""#35433C""/><Setter Property=""BorderThickness"" Value=""1""/>
      <Setter Property=""FontSize"" Value=""14""/><Setter Property=""CaretBrush"" Value=""#43D17A""/>
    </Style>
    <Style x:Key=""IconButton"" TargetType=""Button"">
      <Setter Property=""Width"" Value=""40""/><Setter Property=""Height"" Value=""40""/>
      <Setter Property=""Background"" Value=""Transparent""/><Setter Property=""Foreground"" Value=""#C9D2CD""/>
      <Setter Property=""BorderThickness"" Value=""0""/><Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""FontFamily"" Value=""Segoe MDL2 Assets""/><Setter Property=""FontSize"" Value=""14""/>
      <Setter Property=""Template"">
        <Setter.Value><ControlTemplate TargetType=""Button""><Border x:Name=""Root"" CornerRadius=""6"" Background=""{TemplateBinding Background}""><ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/></Border><ControlTemplate.Triggers><Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""Root"" Property=""Background"" Value=""#26312C""/></Trigger><Trigger Property=""IsPressed"" Value=""True""><Setter TargetName=""Root"" Property=""Opacity"" Value=""0.7""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value>
      </Setter>
    </Style>
    <Style x:Key=""ActionButtonStyle"" TargetType=""Button"">
      <Setter Property=""Height"" Value=""48""/><Setter Property=""Background"" Value=""#43D17A""/>
      <Setter Property=""Foreground"" Value=""#07130C""/><Setter Property=""BorderThickness"" Value=""0""/>
      <Setter Property=""FontSize"" Value=""14""/><Setter Property=""FontWeight"" Value=""SemiBold""/>
      <Setter Property=""Cursor"" Value=""Hand""/>
      <Setter Property=""Template""><Setter.Value><ControlTemplate TargetType=""Button""><Border x:Name=""Root"" CornerRadius=""7"" Background=""{TemplateBinding Background}""><ContentPresenter HorizontalAlignment=""Center"" VerticalAlignment=""Center""/></Border><ControlTemplate.Triggers><Trigger Property=""IsMouseOver"" Value=""True""><Setter TargetName=""Root"" Property=""Opacity"" Value=""0.88""/></Trigger><Trigger Property=""IsPressed"" Value=""True""><Setter TargetName=""Root"" Property=""Opacity"" Value=""0.72""/></Trigger><Trigger Property=""IsEnabled"" Value=""False""><Setter TargetName=""Root"" Property=""Opacity"" Value=""0.45""/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
    </Style>
  </Window.Resources>
  <Grid>
    <Grid.RowDefinitions><RowDefinition Height=""64""/><RowDefinition Height=""*""/></Grid.RowDefinitions>
    <Border x:Name=""TitleBar"" Grid.Row=""0"" Background=""#151A17"" BorderBrush=""#26302B"" BorderThickness=""0,0,0,1"">
      <Grid Margin=""22,0,12,0"">
        <Grid.ColumnDefinitions><ColumnDefinition Width=""*""/><ColumnDefinition Width=""Auto""/></Grid.ColumnDefinitions>
        <StackPanel Orientation=""Horizontal"" VerticalAlignment=""Center"">
          <Border Width=""34"" Height=""34"" CornerRadius=""7"" Background=""#43D17A"" Margin=""0,0,12,0"">
            <Grid><Path Data=""M 8,24 L 8,16 C 8,9 13,5 17,5 C 21,5 26,9 26,16 L 26,24 L 21,24 L 21,16 C 21,12 19,10 17,10 C 15,10 13,12 13,16 L 13,24 Z"" Fill=""#07130C"" Stretch=""Uniform"" Margin=""7""/></Grid>
          </Border>
          <StackPanel VerticalAlignment=""Center""><TextBlock Text=""MineTunnel"" FontSize=""19"" FontWeight=""SemiBold""/><TextBlock Text=""Minecraft Java"" Foreground=""#7F8C85"" FontSize=""11""/></StackPanel>
        </StackPanel>
        <StackPanel Grid.Column=""1"" Orientation=""Horizontal"" VerticalAlignment=""Center"">
          <Button x:Name=""MinimizeButton"" Style=""{StaticResource IconButton}"" Content=""&#xE921;"" ToolTip=""Minimizar""/>
          <Button x:Name=""CloseButton"" Style=""{StaticResource IconButton}"" Content=""&#xE8BB;"" ToolTip=""Fechar""/>
        </StackPanel>
      </Grid>
    </Border>
    <ScrollViewer Grid.Row=""1"" VerticalScrollBarVisibility=""Auto"" HorizontalScrollBarVisibility=""Disabled"">
      <StackPanel Margin=""30,24,30,28"">
        <Grid Margin=""2,0,2,18"">
          <Grid.ColumnDefinitions><ColumnDefinition Width=""*""/><ColumnDefinition Width=""Auto""/></Grid.ColumnDefinitions>
          <StackPanel Orientation=""Horizontal"" VerticalAlignment=""Center"">
            <Ellipse x:Name=""StatusDot"" Width=""10"" Height=""10"" Fill=""#66716B"" Margin=""0,0,10,0""/>
            <StackPanel><TextBlock x:Name=""StatusText"" Text=""Desconectado"" FontSize=""18"" FontWeight=""SemiBold""/><TextBlock x:Name=""StatusDetail"" Text=""Relay dispon&#xED;vel"" Foreground=""#92A098"" FontSize=""12"" Margin=""0,3,0,0""/></StackPanel>
          </StackPanel>
          <TextBlock Grid.Column=""1"" Text=""v0.2"" Foreground=""#66716B"" FontSize=""12"" VerticalAlignment=""Center""/>
        </Grid>
        <Border Background=""{StaticResource PanelBrush}"" BorderBrush=""{StaticResource BorderBrush}"" BorderThickness=""1"" CornerRadius=""8"" Padding=""20"">
          <Grid>
            <Grid.RowDefinitions><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/></Grid.RowDefinitions>
            <TextBlock Text=""PORTA DO MINECRAFT"" Style=""{StaticResource FieldLabel}""/>
            <Grid Grid.Row=""1"" Margin=""0,8,0,16"">
              <Grid.ColumnDefinitions><ColumnDefinition Width=""*""/><ColumnDefinition Width=""12""/><ColumnDefinition Width=""48""/></Grid.ColumnDefinitions>
              <TextBox x:Name=""PortBox"" Style=""{StaticResource InputStyle}"" Text=""25565"" MaxLength=""5"" ToolTip=""Porta local do Minecraft""/>
              <Button x:Name=""DetectButton"" Grid.Column=""2"" Style=""{StaticResource IconButton}"" Background=""#26312C"" Content=""&#xE721;"" ToolTip=""Detectar porta automaticamente""/>
            </Grid>
            <Button x:Name=""ActionButton"" Grid.Row=""2"" Style=""{StaticResource ActionButtonStyle}"">
              <StackPanel Orientation=""Horizontal""><TextBlock x:Name=""ActionIcon"" Text=""&#xE768;"" FontFamily=""Segoe MDL2 Assets"" FontSize=""14"" Margin=""0,0,9,0""/><TextBlock x:Name=""ActionText"" Text=""Iniciar t&#xFA;nel""/></StackPanel>
            </Button>
          </Grid>
        </Border>
        <Border x:Name=""AddressPanel"" Visibility=""Collapsed"" Background=""#173225"" BorderBrush=""#2A8C51"" BorderThickness=""1"" CornerRadius=""8"" Padding=""20"" Margin=""0,14,0,0"">
          <Grid>
            <Grid.ColumnDefinitions><ColumnDefinition Width=""*""/><ColumnDefinition Width=""48""/></Grid.ColumnDefinitions>
            <StackPanel><TextBlock Text=""ENDERE&#xC7;O PARA OS AMIGOS"" Style=""{StaticResource FieldLabel}"" Foreground=""#76D79B""/><TextBlock x:Name=""AddressText"" Text=""relay.exemplo.com:30000"" FontFamily=""Consolas"" FontSize=""21"" FontWeight=""SemiBold"" Margin=""0,7,0,8""/><TextBlock x:Name=""PlayerCountText"" Text=""Nenhuma conex&#xE3;o ativa"" Foreground=""#A9CDB7"" FontSize=""12""/></StackPanel>
            <Button x:Name=""CopyButton"" Grid.Column=""1"" Style=""{StaticResource IconButton}"" Content=""&#xE8C8;"" ToolTip=""Copiar endere&#xE7;o"" VerticalAlignment=""Center""/>
          </Grid>
        </Border>
        <Grid Margin=""2,20,2,8""><TextBlock Text=""ATIVIDADE"" Style=""{StaticResource FieldLabel}""/></Grid>
        <Border Background=""#131915"" BorderBrush=""{StaticResource BorderBrush}"" BorderThickness=""1"" CornerRadius=""8"" Padding=""10,8"" MinHeight=""96"">
          <ListBox x:Name=""ActivityList"" Background=""Transparent"" BorderThickness=""0"" Foreground=""#AEBAB3"" FontFamily=""Consolas"" FontSize=""12"" IsHitTestVisible=""False""/>
        </Border>
        <Expander x:Name=""AdvancedExpander"" Header=""Configura&#xE7;&#xF5;es avan&#xE7;adas"" Foreground=""#C9D2CD"" Margin=""2,18,2,0"">
          <Grid Margin=""0,14,0,0"">
            <Grid.ColumnDefinitions><ColumnDefinition Width=""*""/><ColumnDefinition Width=""16""/><ColumnDefinition Width=""*""/></Grid.ColumnDefinitions>
            <Grid.RowDefinitions><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/><RowDefinition Height=""14""/><RowDefinition Height=""Auto""/><RowDefinition Height=""Auto""/></Grid.RowDefinitions>
            <TextBlock Text=""RELAY"" Style=""{StaticResource FieldLabel}""/><TextBlock Grid.Column=""2"" Text=""NOME DO PC"" Style=""{StaticResource FieldLabel}""/>
            <TextBox x:Name=""RelayBox"" Grid.Row=""1"" Style=""{StaticResource InputStyle}"" Margin=""0,7,0,0""/><TextBox x:Name=""NameBox"" Grid.Row=""1"" Grid.Column=""2"" Style=""{StaticResource InputStyle}"" Margin=""0,7,0,0""/>
            <TextBlock Grid.Row=""3"" Grid.ColumnSpan=""3"" Text=""SEGREDO DO RELAY"" Style=""{StaticResource FieldLabel}""/>
            <PasswordBox x:Name=""SecretBox"" Grid.Row=""4"" Grid.ColumnSpan=""3"" Style=""{StaticResource PasswordStyle}"" Margin=""0,7,0,0""/>
          </Grid>
        </Expander>
      </StackPanel>
    </ScrollViewer>
  </Grid>
</Window>";

        private readonly string configPath;
        private readonly object clientsLock = new object();
        private readonly List<TcpClient> activeClients = new List<TcpClient>();

        private TextBox portBox;
        private TextBox relayBox;
        private TextBox nameBox;
        private PasswordBox secretBox;
        private Button actionButton;
        private Button detectButton;
        private TextBlock actionIcon;
        private TextBlock actionText;
        private Ellipse statusDot;
        private TextBlock statusText;
        private TextBlock statusDetail;
        private Border addressPanel;
        private TextBlock addressText;
        private TextBlock playerCountText;
        private ListBox activityList;

        private TunnelState state;
        private CancellationTokenSource cancellation;
        private TcpClient controlClient;
        private StreamReader controlReader;
        private NetworkStream controlStream;
        private string relayHost;
        private int relayPort;
        private string sessionToken;
        private int activePlayers;
        private bool screenshotMode;

        public Window Window { get; private set; }

        public MainController()
        {
            configPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "minetunnel.json");
            Window = (Window)XamlReader.Parse(Xaml);
            BindControls();
            BindEvents();
            LoadConfig();
            SetIdleUi();
            AddActivity("Pronto para iniciar.");
        }

        private void BindControls()
        {
            portBox = (TextBox)Window.FindName("PortBox");
            relayBox = (TextBox)Window.FindName("RelayBox");
            nameBox = (TextBox)Window.FindName("NameBox");
            secretBox = (PasswordBox)Window.FindName("SecretBox");
            actionButton = (Button)Window.FindName("ActionButton");
            detectButton = (Button)Window.FindName("DetectButton");
            actionIcon = (TextBlock)Window.FindName("ActionIcon");
            actionText = (TextBlock)Window.FindName("ActionText");
            statusDot = (Ellipse)Window.FindName("StatusDot");
            statusText = (TextBlock)Window.FindName("StatusText");
            statusDetail = (TextBlock)Window.FindName("StatusDetail");
            addressPanel = (Border)Window.FindName("AddressPanel");
            addressText = (TextBlock)Window.FindName("AddressText");
            playerCountText = (TextBlock)Window.FindName("PlayerCountText");
            activityList = (ListBox)Window.FindName("ActivityList");
        }

        private void BindEvents()
        {
            ((Button)Window.FindName("CloseButton")).Click += delegate { Window.Close(); };
            ((Button)Window.FindName("MinimizeButton")).Click += delegate { Window.WindowState = WindowState.Minimized; };
            ((Border)Window.FindName("TitleBar")).MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                DependencyObject source = e.OriginalSource as DependencyObject;
                while (source != null)
                {
                    if (source is Button) return;
                    source = VisualTreeHelper.GetParent(source);
                }
                if (e.ClickCount == 2)
                {
                    Window.WindowState = Window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                }
                else
                {
                    Window.DragMove();
                }
            };
            actionButton.Click += ActionButtonClick;
            detectButton.Click += DetectButtonClick;
            ((Button)Window.FindName("CopyButton")).Click += CopyButtonClick;
            portBox.PreviewTextInput += delegate(object sender, TextCompositionEventArgs e)
            {
                e.Handled = e.Text.Any(delegate(char c) { return !char.IsDigit(c); });
            };
            Window.Closing += delegate
            {
                if (!screenshotMode)
                {
                    SaveConfig();
                }
                StopTunnel(false);
            };
        }

        private async void ActionButtonClick(object sender, RoutedEventArgs e)
        {
            if (state == TunnelState.Active || state == TunnelState.Connecting)
            {
                StopTunnel(true);
                return;
            }
            await StartTunnelAsync();
        }

        private async Task StartTunnelAsync()
        {
            int localPort;
            string host;
            int port;
            if (!int.TryParse(portBox.Text, out localPort) || localPort < 1 || localPort > 65535)
            {
                SetErrorUi("Porta inv\u00E1lida", "Use um valor entre 1 e 65535.");
                return;
            }
            if (!TryParseEndpoint(relayBox.Text.Trim(), out host, out port))
            {
                SetErrorUi("Relay inv\u00E1lido", "Confira o endere\u00E7o nas configura\u00E7\u00F5es avan\u00E7adas.");
                return;
            }
            if (string.IsNullOrWhiteSpace(secretBox.Password))
            {
                SetErrorUi("Segredo ausente", "Preencha o segredo do relay.");
                return;
            }

            SaveConfig();
            SetConnectingUi();
            AddActivity("Verificando Minecraft em 127.0.0.1:" + localPort + "...");
            cancellation = new CancellationTokenSource();
            CancellationToken token = cancellation.Token;

            try
            {
                TcpClient probe = new TcpClient();
                try
                {
                    await ConnectWithTimeout(probe, "127.0.0.1", localPort, 1800, token);
                }
                finally
                {
                    probe.Close();
                }

                AddActivity("Conectando ao relay...");
                controlClient = new TcpClient();
                controlClient.NoDelay = true;
                await ConnectWithTimeout(controlClient, host, port, 6000, token);
                controlStream = controlClient.GetStream();
                controlReader = new StreamReader(controlStream, new UTF8Encoding(false), false, 4096, true);

                ProtocolMessage registration = new ProtocolMessage();
                registration.type = "register";
                registration.secret = secretBox.Password;
                registration.name = string.IsNullOrWhiteSpace(nameBox.Text) ? Environment.MachineName : nameBox.Text.Trim();
                await WriteMessageAsync(controlStream, registration, token);

                string line = await ReadLineWithTimeout(controlReader, 6000, token);
                ProtocolMessage response = Deserialize(line);
                if (response == null || response.type == "error")
                {
                    throw new InvalidOperationException(response == null ? "Resposta inv\u00E1lida do relay." : response.error);
                }
                if (response.type != "registered" || string.IsNullOrWhiteSpace(response.token))
                {
                    throw new InvalidOperationException("O relay recusou o registro do t\u00FAnel.");
                }

                relayHost = host;
                relayPort = port;
                sessionToken = response.token;
                string publicAddress = response.public_host + ":" + response.public_port;
                SetActiveUi(publicAddress);
                AddActivity("T\u00FAnel publicado em " + publicAddress + ".");
                Task ignored = ControlLoopAsync(localPort, token);
            }
            catch (OperationCanceledException)
            {
                SetIdleUi();
            }
            catch (Exception ex)
            {
                CloseNetworkResources();
                SetErrorUi("N\u00E3o foi poss\u00EDvel iniciar", FriendlyError(ex));
                AddActivity("Falha: " + FriendlyError(ex));
            }
        }

        private async Task ControlLoopAsync(int localPort, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    string line = await controlReader.ReadLineAsync();
                    if (line == null)
                    {
                        throw new IOException("A conex\u00E3o com o relay foi encerrada.");
                    }
                    ProtocolMessage message = Deserialize(line);
                    if (message == null)
                    {
                        continue;
                    }
                    if (message.type == "open" && !string.IsNullOrWhiteSpace(message.connection_id))
                    {
                        Task ignored = HandleIncomingConnectionAsync(message.connection_id, localPort, token);
                    }
                    else if (message.type == "error")
                    {
                        throw new IOException(message.error);
                    }
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    OnUi(delegate
                    {
                        CloseNetworkResources();
                        SetErrorUi("Conex\u00E3o encerrada", FriendlyError(ex));
                        AddActivity("Relay desconectado.");
                    });
                }
            }
        }

        private async Task HandleIncomingConnectionAsync(string connectionId, int localPort, CancellationToken token)
        {
            TcpClient dataClient = new TcpClient();
            TcpClient localClient = new TcpClient();
            bool counted = false;
            AddClient(dataClient);
            AddClient(localClient);
            try
            {
                dataClient.NoDelay = true;
                localClient.NoDelay = true;
                await ConnectWithTimeout(dataClient, relayHost, relayPort, 6000, token);
                NetworkStream relayStream = dataClient.GetStream();
                ProtocolMessage handshake = new ProtocolMessage();
                handshake.type = "data";
                handshake.token = sessionToken;
                handshake.connection_id = connectionId;
                await WriteMessageAsync(relayStream, handshake, token);

                await ConnectWithTimeout(localClient, "127.0.0.1", localPort, 3000, token);
                NetworkStream localStream = localClient.GetStream();
                counted = true;
                UpdatePlayerCount(1);
                AddActivity("Jogador conectado.");

                Task upstream = localStream.CopyToAsync(relayStream, 81920, token);
                Task downstream = relayStream.CopyToAsync(localStream, 81920, token);
                await Task.WhenAny(upstream, downstream);
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    AddActivity("Conex\u00E3o de jogador encerrada: " + FriendlyError(ex));
                }
            }
            finally
            {
                dataClient.Close();
                localClient.Close();
                RemoveClient(dataClient);
                RemoveClient(localClient);
                if (counted)
                {
                    UpdatePlayerCount(-1);
                    AddActivity("Jogador desconectado.");
                }
            }
        }

        private void StopTunnel(bool log)
        {
            CancellationTokenSource current = cancellation;
            cancellation = null;
            if (current != null)
            {
                current.Cancel();
            }
            CloseNetworkResources();
            if (log && state != TunnelState.Idle)
            {
                AddActivity("T\u00FAnel parado.");
            }
            SetIdleUi();
        }

        private void CloseNetworkResources()
        {
            try { if (controlClient != null) controlClient.Close(); } catch { }
            controlClient = null;
            controlReader = null;
            controlStream = null;
            sessionToken = null;

            lock (clientsLock)
            {
                foreach (TcpClient client in activeClients.ToArray())
                {
                    try { client.Close(); } catch { }
                }
                activeClients.Clear();
            }
            Interlocked.Exchange(ref activePlayers, 0);
            UpdatePlayerCountText(0);
        }

        private async void DetectButtonClick(object sender, RoutedEventArgs e)
        {
            detectButton.IsEnabled = false;
            try
            {
                int[] ports = await Task.Run<int[]>(delegate { return FindJavaListeningPorts(); });
                if (ports.Length == 0)
                {
                    AddActivity("Nenhuma porta Java foi encontrada.");
                    statusDetail.Text = "Minecraft n\u00E3o detectado";
                    return;
                }
                int selected = ports.Contains(25565) ? 25565 : ports.OrderByDescending(delegate(int p) { return p; }).First();
                portBox.Text = selected.ToString();
                statusDetail.Text = "Porta " + selected + " detectada";
                AddActivity("Porta detectada: " + selected + ".");
            }
            catch (Exception ex)
            {
                AddActivity("Detec\u00E7\u00E3o falhou: " + FriendlyError(ex));
            }
            finally
            {
                detectButton.IsEnabled = true;
            }
        }

        private void CopyButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(addressText.Text);
                statusDetail.Text = "Endere\u00E7o copiado";
                AddActivity("Endere\u00E7o copiado.");
            }
            catch (Exception ex)
            {
                AddActivity("N\u00E3o foi poss\u00EDvel copiar: " + FriendlyError(ex));
            }
        }

        private int[] FindJavaListeningPorts()
        {
            HashSet<int> processIds = new HashSet<int>();
            foreach (string processName in new[] { "java", "javaw" })
            {
                foreach (Process process in Process.GetProcessesByName(processName))
                {
                    try { processIds.Add(process.Id); } catch { }
                    process.Dispose();
                }
            }
            if (processIds.Count == 0)
            {
                return new int[0];
            }

            ProcessStartInfo info = new ProcessStartInfo("netstat.exe", "-ano -p tcp");
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            Process netstat = Process.Start(info);
            string output = netstat.StandardOutput.ReadToEnd();
            netstat.WaitForExit(5000);
            netstat.Dispose();

            Regex linePattern = new Regex(@"^\s*TCP\s+\S+:(\d+)\s+\S+\s+LISTENING\s+(\d+)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            HashSet<int> ports = new HashSet<int>();
            foreach (Match match in linePattern.Matches(output))
            {
                int port;
                int pid;
                if (int.TryParse(match.Groups[1].Value, out port) && int.TryParse(match.Groups[2].Value, out pid) && processIds.Contains(pid))
                {
                    ports.Add(port);
                }
            }
            return ports.OrderBy(delegate(int p) { return p; }).ToArray();
        }

        private void LoadConfig()
        {
            AppConfig config = null;
            try
            {
                if (File.Exists(configPath))
                {
                    config = new JavaScriptSerializer().Deserialize<AppConfig>(File.ReadAllText(configPath, Encoding.UTF8));
                }
            }
            catch
            {
                config = null;
            }
            if (config == null)
            {
                config = new AppConfig();
                config.relay = "127.0.0.1:7000";
                config.local = "127.0.0.1:25565";
                config.secret = "";
                config.name = Environment.MachineName;
            }

            relayBox.Text = string.IsNullOrWhiteSpace(config.relay) ? "127.0.0.1:7000" : config.relay;
            nameBox.Text = string.IsNullOrWhiteSpace(config.name) ? Environment.MachineName : config.name;
            secretBox.Password = config.secret ?? "";
            string host;
            int port;
            if (TryParseEndpoint(config.local, out host, out port))
            {
                portBox.Text = port.ToString();
            }
        }

        private void SaveConfig()
        {
            if (screenshotMode)
            {
                return;
            }
            try
            {
                AppConfig config = new AppConfig();
                config.relay = relayBox.Text.Trim();
                config.local = "127.0.0.1:" + portBox.Text.Trim();
                config.secret = secretBox.Password;
                config.name = string.IsNullOrWhiteSpace(nameBox.Text) ? Environment.MachineName : nameBox.Text.Trim();
                string json = new JavaScriptSerializer().Serialize(config);
                File.WriteAllText(configPath, json, new UTF8Encoding(false));
            }
            catch (Exception ex)
            {
                AddActivity("Configura\u00E7\u00E3o n\u00E3o salva: " + FriendlyError(ex));
            }
        }

        private void SetIdleUi()
        {
            state = TunnelState.Idle;
            statusDot.Fill = Brush("#66716B");
            statusText.Text = "Desconectado";
            statusDetail.Text = "Relay dispon\u00EDvel";
            actionButton.IsEnabled = true;
            actionButton.Background = Brush("#43D17A");
            actionButton.Foreground = Brush("#07130C");
            actionIcon.Text = "\uE768";
            actionText.Text = "Iniciar t\u00FAnel";
            portBox.IsEnabled = true;
            detectButton.IsEnabled = true;
            addressPanel.Visibility = Visibility.Collapsed;
        }

        private void SetConnectingUi()
        {
            state = TunnelState.Connecting;
            statusDot.Fill = Brush("#E0B64B");
            statusText.Text = "Conectando";
            statusDetail.Text = "Validando Minecraft e relay";
            actionButton.IsEnabled = true;
            actionButton.Background = Brush("#3A423E");
            actionButton.Foreground = Brush("#F3F7F4");
            actionIcon.Text = "\uE71A";
            actionText.Text = "Cancelar";
            portBox.IsEnabled = false;
            detectButton.IsEnabled = false;
            addressPanel.Visibility = Visibility.Collapsed;
        }

        private void SetActiveUi(string address)
        {
            state = TunnelState.Active;
            statusDot.Fill = Brush("#43D17A");
            statusText.Text = "T\u00FAnel ativo";
            statusDetail.Text = "Pronto para receber jogadores";
            actionButton.Background = Brush("#43282B");
            actionButton.Foreground = Brush("#FFD9DC");
            actionIcon.Text = "\uE71A";
            actionText.Text = "Parar t\u00FAnel";
            portBox.IsEnabled = false;
            detectButton.IsEnabled = false;
            addressText.Text = address;
            addressPanel.Visibility = Visibility.Visible;
            UpdatePlayerCountText(0);
        }

        private void SetErrorUi(string title, string detail)
        {
            state = TunnelState.Error;
            statusDot.Fill = Brush("#F06A70");
            statusText.Text = title;
            statusDetail.Text = detail;
            actionButton.IsEnabled = true;
            actionButton.Background = Brush("#43D17A");
            actionButton.Foreground = Brush("#07130C");
            actionIcon.Text = "\uE768";
            actionText.Text = "Tentar novamente";
            portBox.IsEnabled = true;
            detectButton.IsEnabled = true;
            addressPanel.Visibility = Visibility.Collapsed;
        }

        private void UpdatePlayerCount(int delta)
        {
            int count = Interlocked.Add(ref activePlayers, delta);
            if (count < 0)
            {
                count = Interlocked.Exchange(ref activePlayers, 0);
            }
            UpdatePlayerCountText(count);
        }

        private void UpdatePlayerCountText(int count)
        {
            OnUi(delegate
            {
                if (count == 0) playerCountText.Text = "Nenhuma conex\u00E3o ativa";
                else if (count == 1) playerCountText.Text = "1 jogador conectado";
                else playerCountText.Text = count + " jogadores conectados";
            });
        }

        private void AddActivity(string text)
        {
            OnUi(delegate
            {
                activityList.Items.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "  " + text);
                while (activityList.Items.Count > 5)
                {
                    activityList.Items.RemoveAt(activityList.Items.Count - 1);
                }
            });
        }

        private void AddClient(TcpClient client)
        {
            lock (clientsLock) activeClients.Add(client);
        }

        private void RemoveClient(TcpClient client)
        {
            lock (clientsLock) activeClients.Remove(client);
        }

        private void OnUi(Action action)
        {
            if (Window.Dispatcher.CheckAccess()) action();
            else Window.Dispatcher.BeginInvoke(action);
        }

        private static async Task ConnectWithTimeout(TcpClient client, string host, int port, int timeoutMilliseconds, CancellationToken token)
        {
            Task connect = client.ConnectAsync(host, port);
            Task timeout = Task.Delay(timeoutMilliseconds, token);
            Task completed = await Task.WhenAny(connect, timeout);
            token.ThrowIfCancellationRequested();
            if (completed != connect)
            {
                throw new TimeoutException("Tempo limite de conex\u00E3o excedido.");
            }
            await connect;
        }

        private static async Task<string> ReadLineWithTimeout(StreamReader reader, int timeoutMilliseconds, CancellationToken token)
        {
            Task<string> read = reader.ReadLineAsync();
            Task timeout = Task.Delay(timeoutMilliseconds, token);
            Task completed = await Task.WhenAny(read, timeout);
            token.ThrowIfCancellationRequested();
            if (completed != read)
            {
                throw new TimeoutException("O relay n\u00E3o respondeu a tempo.");
            }
            string line = await read;
            if (line == null) throw new IOException("O relay encerrou a conex\u00E3o.");
            return line;
        }

        private static async Task WriteMessageAsync(NetworkStream stream, ProtocolMessage message, CancellationToken token)
        {
            string json = new JavaScriptSerializer().Serialize(message) + "\n";
            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            await stream.WriteAsync(bytes, 0, bytes.Length, token);
            await stream.FlushAsync(token);
        }

        private static ProtocolMessage Deserialize(string json)
        {
            try { return new JavaScriptSerializer().Deserialize<ProtocolMessage>(json); }
            catch { return null; }
        }

        private static bool TryParseEndpoint(string value, out string host, out int port)
        {
            host = null;
            port = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            int separator = value.LastIndexOf(':');
            if (separator < 1 || separator == value.Length - 1) return false;
            host = value.Substring(0, separator).Trim();
            return host.Length > 0 && int.TryParse(value.Substring(separator + 1), out port) && port > 0 && port <= 65535;
        }

        private static string FriendlyError(Exception ex)
        {
            Exception current = ex;
            while (current.InnerException != null) current = current.InnerException;
            if (current is SocketException)
            {
                SocketException socket = (SocketException)current;
                if (socket.SocketErrorCode == SocketError.ConnectionRefused)
                    return "Minecraft ou relay n\u00E3o est\u00E1 aceitando conex\u00F5es nessa porta.";
                if (socket.SocketErrorCode == SocketError.TimedOut)
                    return "A conex\u00E3o demorou demais para responder.";
            }
            return string.IsNullOrWhiteSpace(current.Message) ? "Erro de conex\u00E3o." : current.Message;
        }

        private static Brush Brush(string color)
        {
            return (Brush)new BrushConverter().ConvertFromString(color);
        }

        public void PrepareScreenshot(bool active)
        {
            screenshotMode = true;
            relayBox.Text = "relay.exemplo.com:7000";
            portBox.Text = "25565";
            if (active)
            {
                SetActiveUi("relay.exemplo.com:30000");
                UpdatePlayerCountText(2);
                activityList.Items.Clear();
                activityList.Items.Add("18:42:18  Jogador conectado.");
                activityList.Items.Add("18:41:55  T\u00FAnel publicado em relay.exemplo.com:30000.");
                activityList.Items.Add("18:41:54  Conectando ao relay...");
            }
        }

        public void RenderScreenshot(string path)
        {
            Window.UpdateLayout();
            int width = Math.Max(1, (int)Math.Ceiling(Window.ActualWidth));
            int height = Math.Max(1, (int)Math.Ceiling(Window.ActualHeight));
            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(Window);
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream output = File.Create(path)) encoder.Save(output);
        }

        public Task StartForAutomation()
        {
            return StartTunnelAsync();
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainController controller;
            try
            {
                controller = new MainController();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "MineTunnel", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool screenshot = args.Length >= 2 && (args[0] == "--screenshot" || args[0] == "--screenshot-active");
            if (screenshot)
            {
                controller.PrepareScreenshot(args[0] == "--screenshot-active");
                controller.Window.ShowInTaskbar = false;
                controller.Window.WindowStartupLocation = WindowStartupLocation.Manual;
                controller.Window.Left = -10000;
                controller.Window.Top = -10000;
                controller.Window.Loaded += delegate
                {
                    controller.Window.Dispatcher.BeginInvoke(new Action(delegate
                    {
                        controller.RenderScreenshot(args[1]);
                        controller.Window.Close();
                    }), DispatcherPriority.ApplicationIdle);
                };
            }
            else if (args.Length >= 1 && args[0] == "--autostart")
            {
                controller.Window.ShowInTaskbar = false;
                controller.Window.WindowStartupLocation = WindowStartupLocation.Manual;
                controller.Window.Left = -10000;
                controller.Window.Top = -10000;
                controller.Window.Loaded += async delegate { await controller.StartForAutomation(); };
            }
            app.Run(controller.Window);
        }
    }
}
