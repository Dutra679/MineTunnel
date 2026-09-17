# Contribuindo

Obrigado por considerar uma contribuicao ao MineTunnel.

## Ambiente

- Relay: Go 1.22 ou mais recente.
- Aplicativo: Windows 10/11, .NET Framework 4.8 e PowerShell.

## Fluxo sugerido

1. Crie uma branch a partir de `main`.
2. Mantenha a alteracao pequena e focada.
3. Nao inclua credenciais, IPs operacionais ou binarios privados.
4. Execute as verificacoes aplicaveis.
5. Explique comportamento, riscos e testes no pull request.

```bash
cd relay
go test ./...
go vet ./...
```

```powershell
powershell -ExecutionPolicy Bypass -File .\app\build.ps1
```

## Padroes

- Preserve compatibilidade do protocolo ou documente a quebra.
- Prefira biblioteca padrao no relay enquanto ela atender ao caso.
- Mantenha mensagens da interface claras e em portugues.
- Adicione logs uteis sem registrar segredos ou tokens.
- Inclua testes para validacoes, concorrencia e ciclo de vida ao alterar o relay.
- Atualize README, protocolo e changelog quando o comportamento mudar.

## Commits

Mensagens curtas no imperativo ou no formato Conventional Commits sao bem-vindas:

```text
feat: adiciona reconexao automatica
fix: libera porta apos falha no registro
docs: detalha configuracao do firewall
```
