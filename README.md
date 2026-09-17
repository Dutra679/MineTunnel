# MineTunnel

MineTunnel publica um mundo Minecraft Java aberto para LAN por meio de uma VPS.
Somente o computador que hospeda o mundo executa o aplicativo; os outros
jogadores entram pelo Minecraft normal usando o endereco publico exibido.

## Estado do projeto

Este e um MVP funcional para Minecraft Java (TCP):

- aplicativo Windows com interface grafica;
- deteccao da porta local do Minecraft;
- relay Linux com portas publicas alocadas dinamicamente;
- varios jogadores por tunel;
- conexao reversa, sem encaminhamento de porta no roteador.

![Interface do MineTunnel](app/active-final.png)

O protocolo ainda usa um segredo compartilhado e trafego sem TLS. Nao use o
relay para dados sensiveis nem publique uma configuracao real.

## Como funciona

```text
Minecraft dos amigos
        |
        v
VPS publica (relay TCP)
        ^
        | conexao de saida
MineTunnel no PC anfitriao -> mundo Minecraft local
```

O relay nao hospeda nem mantem o mundo ativo. O PC anfitriao, o Minecraft e o
MineTunnel precisam permanecer abertos enquanto os jogadores estiverem online.

## Aplicativo Windows

Requisitos para compilar:

- Windows 10 ou 11;
- .NET Framework 4.8;
- Windows PowerShell.

Compile com:

```powershell
powershell -ExecutionPolicy Bypass -File .\app\build.ps1
```

Na primeira compilacao, o script cria `app\dist\minetunnel.json` a partir do
exemplo. Preencha o endereco do relay e o mesmo segredo configurado no servidor.

Para usar:

1. Abra um mundo no Minecraft Java e selecione **Abrir para LAN**.
2. Abra `MineTunnel.exe` no computador anfitriao.
3. Detecte ou informe a porta LAN mostrada pelo Minecraft.
4. Inicie o tunel e envie aos amigos o endereco publico exibido.

## Relay Linux

Requisitos:

- Go 1.20 ou mais recente para compilar;
- uma VPS Linux com IP publico;
- TCP `7000` e o intervalo TCP `30000-30100` liberados no provedor e no firewall.

Compile o relay:

```bash
cd relay
go build -o minetunnel-relay .
```

Para uma instalacao manual, execute o binario informando o IP ou dominio publico:

```bash
export MINETUNNEL_SECRET="troque-por-um-segredo-longo-e-aleatorio"
./minetunnel-relay \
  --listen=:7000 \
  --public-host=SEU_IP_PUBLICO \
  --port-start=30000 \
  --port-end=30100
```

Os arquivos em `deploy/` fornecem um exemplo de servico systemd. Depois de
enviar o binario e o servico para os caminhos esperados pelo script, execute:

```bash
sudo sh install-relay.sh SEU_IP_PUBLICO
```

O instalador cria um segredo aleatorio na primeira execucao e o armazena em
`/etc/minetunnel-relay` com permissao restrita.

## Seguranca

- Nunca envie `minetunnel.json`, chaves SSH ou arquivos `.zip` privados ao Git.
- Distribua a configuracao real somente por um canal privado.
- Cada instalacao publica deve usar um segredo novo e aleatorio.
- Para uso aberto ao publico, implemente credenciais individuais, TLS, limites
  de conexao e revogacao de acesso.

## Limitacoes atuais

- somente Minecraft Java/TCP;
- sem criptografia TLS entre cliente e relay;
- um segredo compartilhado por relay;
- sem atualizacao automatica ou instalador do Windows;
- o relay nao substitui um servidor Minecraft 24 horas.
