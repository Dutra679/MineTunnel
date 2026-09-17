# Politica de seguranca

## Estado atual

MineTunnel e um MVP funcional, nao um servico de tunel com seguranca auditada.
Use em grupos pequenos e controlados.

Riscos conhecidos:

- controle e dados nao usam TLS;
- o segredo e compartilhado por todos os clientes de um relay;
- `minetunnel.json` guarda o segredo em texto simples;
- nao existem limites por identidade, revogacao individual ou auditoria;
- portas publicas ficam expostas a varreduras e tentativas de abuso;
- o relay nao valida nem filtra o protocolo Minecraft.

## Dados que nunca devem ser publicados

- `minetunnel.json` real;
- `/etc/minetunnel-relay`;
- chaves SSH privadas;
- pacotes ZIP preparados para amigos;
- logs contendo enderecos ou informacoes que voce considere privadas.

O `.gitignore` cobre os caminhos usados neste repositorio, mas ele nao substitui
uma revisao antes de cada commit.

## Reportando uma vulnerabilidade

Nao abra uma issue publica contendo uma credencial, exploit funcional ou dados
de terceiros. Entre em contato de forma privada com o mantenedor pelo perfil do
GitHub e inclua:

- versao ou commit afetado;
- impacto observado;
- passos minimos para reproducao;
- sugestao de correcao, quando houver.

Remova segredos e dados pessoais antes de enviar qualquer evidencia.

## Resposta a incidente

Se uma credencial do relay for exposta:

1. substitua `MINETUNNEL_SECRET` imediatamente;
2. reinicie o servico;
3. atualize apenas os clientes autorizados;
4. remova artefatos publicos que contenham a credencial;
5. considere o segredo antigo permanentemente comprometido.

Remover um segredo do commit mais recente nao o remove do historico Git. Nessa
situacao, rotacione primeiro e depois reescreva o historico quando necessario.
