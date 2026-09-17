# Solucao de problemas

## O aplicativo nao encontra a porta

1. Confirme que o mundo foi aberto para LAN.
2. Leia no chat do Minecraft a porta exibida.
3. Informe essa porta manualmente no aplicativo.
4. Verifique se o Java esta ouvindo:

```powershell
netstat -ano -p tcp | Select-String LISTENING
```

A deteccao procura processos chamados `java` e `javaw`. Launchers que usam outro
nome de processo podem exigir entrada manual.

## Minecraft ou relay nao aceita conexoes

O aplicativo testa primeiro o Minecraft local e depois o relay. Verifique:

- se o mundo continua aberto para LAN;
- se a porta informada e a da sessao atual;
- se `relay` no JSON aponta para a porta de controle;
- se o servico Linux esta ativo;
- se o provedor e o sistema operacional permitem a porta.

## Autenticacao falhou

O valor de `secret` no cliente precisa ser exatamente o valor de
`MINETUNNEL_SECRET` no relay. Evite espacos extras e nao publique nenhum dos
dois arquivos.

## O anfitriao conecta, mas os amigos nao

Teste o endereco publico a partir de outra rede. A porta publica so fica aberta
enquanto o tunel esta ativo.

No relay:

```bash
sudo journalctl -u minetunnel-relay -f
sudo ss -lntp
```

Se o listener existe mas nao e alcancavel externamente, revise a lista de
seguranca do provedor, a tabela de rotas, o gateway de internet e o firewall da
VPS.

## A conexao cai depois de algum tempo

Confira, nesta ordem:

1. log de atividade do aplicativo;
2. log do servico systemd;
3. estabilidade do upload do anfitriao;
4. uso de memoria, CPU e rede da VPS;
5. suspensao ou economia de energia do Windows;
6. encerramento do mundo LAN pelo Minecraft.

O MVP nao reconecta automaticamente. Depois de uma queda, pare e inicie o tunel
novamente.

## A porta publica mudou

As portas sao alocadas dinamicamente e liberadas quando a sessao termina. Um
novo registro pode receber outra porta. Sempre compartilhe o endereco mostrado
na sessao atual.
