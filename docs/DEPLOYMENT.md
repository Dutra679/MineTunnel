# Implantacao do relay

Este guia usa Ubuntu e systemd, mas o binario pode executar em outras
distribuicoes Linux.

## 1. Preparar a VPS

Use uma instancia com IP publico e permita entrada TCP nas portas:

- `22` para administracao SSH;
- `7000` para controle e canais de dados;
- `30000-30100` para tuneis publicos.

As portas precisam estar liberadas em duas camadas quando ambas existirem:

1. firewall ou lista de seguranca do provedor;
2. firewall do sistema operacional.

Exemplo com UFW:

```bash
sudo ufw allow 22/tcp
sudo ufw allow 7000/tcp
sudo ufw allow 30000:30100/tcp
sudo ufw enable
```

Nao remova a regra SSH durante uma sessao remota.

## 2. Compilar

Na propria VPS:

```bash
git clone URL_DO_SEU_REPOSITORIO
cd MineTunnel/relay
go test ./...
go build -trimpath -ldflags="-s -w" -o minetunnel-relay .
```

Ou gere um binario para Linux a partir de outra maquina:

```bash
GOOS=linux GOARCH=amd64 go build -trimpath -o minetunnel-relay-linux-amd64 ./relay
GOOS=linux GOARCH=arm64 go build -trimpath -o minetunnel-relay-linux-arm64 ./relay
```

## 3. Instalar com systemd

O script em `deploy/install-relay.sh` espera, por padrao:

- binario em `/home/ubuntu/minetunnel-relay-src/minetunnel-relay-linux-amd64`;
- unidade em `/home/ubuntu/minetunnel-relay.service`.

Depois de enviar os arquivos:

```bash
sudo sh install-relay.sh SEU_IP_OU_DOMINIO_PUBLICO
```

Na primeira execucao, o instalador:

1. copia o binario para `/usr/local/bin/minetunnel-relay`;
2. gera 24 bytes aleatorios com OpenSSL;
3. grava host e segredo em `/etc/minetunnel-relay` com modo `0600`;
4. instala e inicia `minetunnel-relay.service`;
5. mostra o segredo uma unica vez para configurar os clientes.

Execucoes posteriores preservam o segredo existente.

## 4. Verificar

```bash
sudo systemctl status minetunnel-relay --no-pager
sudo journalctl -u minetunnel-relay -n 100 --no-pager
sudo ss -lntp | grep -E ':7000|:300[0-9][0-9]'
```

Antes de existir um tunel, somente `7000` deve estar em escuta. Uma porta do
intervalo aparece quando um aplicativo se registra.

Teste a conectividade a partir de outra rede:

```bash
nc -vz SEU_IP_OU_DOMINIO 7000
```

## Atualizacao

Compile e envie o novo binario para um caminho temporario, depois execute:

```bash
sudo install -m 0755 ./minetunnel-relay /usr/local/bin/minetunnel-relay
sudo systemctl restart minetunnel-relay
sudo systemctl status minetunnel-relay --no-pager
```

O reinicio encerra tuneis ativos. Avise os anfitrioes antes da manutencao.

## Backup e recuperacao

O relay nao armazena mundos nem dados persistentes de jogadores. O unico dado
operacional que precisa ser preservado e `/etc/minetunnel-relay`.

Se o segredo for exposto:

1. gere um novo valor aleatorio;
2. atualize `/etc/minetunnel-relay`;
3. reinicie o servico;
4. distribua novas configuracoes por canal privado;
5. invalide e remova pacotes antigos.

## Hardening recomendado

- execute como usuario sem privilegios;
- mantenha `NoNewPrivileges` e as protecoes do unit file;
- aplique atualizacoes de seguranca no sistema;
- limite conexoes no firewall quando houver uma lista conhecida de origens;
- monitore descritores, banda, memoria e reinicios;
- nao grave o segredo em argumentos de processo ou repositorios;
- use TLS antes de operar como servico aberto ao publico.
