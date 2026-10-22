# Validação executada em 22/10/2026

| Verificação | Resultado |
|---|---|
| `bash scripts/setup.sh` no checkout real | Passou; instalação/ativação do SDK, hashes das referências oficiais e restores com lockfiles |
| `dotnet build RPCore.csproj -c Release --no-restore` | Passou; .NET Framework 4.8, zero erros e zero avisos |
| `dotnet test tests/RPCore.Tests.csproj -c Release --no-restore` | **31 passaram, 0 falharam, 0 ignorados** |
| Importação dos três saves reais enviados | Passou; fome/hidratação/saldo e cópia exata dos 151 campos de cada JSON |
| Corrupção, escrita atômica/backup e migração única | Passou; arquivos originais preservados e defaults impedidos de sobrescrever JSON inválido |
| Transferências e economia | Passou; conservação de itens, capacidade/saldo, custo médio, rejeição de valores inválidos e overflow |
| Instalador PowerShell | Parser sem erros; duas execuções reais com PowerShell 7.6.7/Linux em pasta temporária com estrutura de jogo; dados e DLLs existentes preservados, backups conferidos |
| Cópia duplicada da DLL do mod | Instalador recusou antes de alterar arquivos |
| Arquivos PSRP originais | 62 arquivos conferidos byte a byte na base e no pacote; sem conversão ou regravação |
| Distribuição | CRC do ZIP e SHA-256 de todos os arquivos do manifesto conferidos |

O relatório detalhado do runner está em `DataTests.trx`, e a validação do instalador está em `installer-validation.json`. Os testes ligam os mesmos arquivos `Persistence.cs` e `Economy.cs` usados no mod, sem substituir a API GTA por stubs. Os saves usados como fixtures são os enviados pelo usuário.

DLL compilada, SHA-256: `07a28a07b1aa52fc73b98d1c01b7ce2ec55e2bdd3bac7645e306854c79716f00`.

**Não executados:** GTA V, hooks/natives, menus e marcadores dentro do jogo, reprodução de áudio/rádio COM e instalador no Windows. O teste do instalador valida operações de arquivo no PowerShell/Linux, sem iniciar o GTA nem afirmar compatibilidade completa do Windows. Siga `TESTES-GTA.md`.

O ambiente cloud possui SDK e referências preparados. `install_script` e `start_skill` foram salvos como rascunho da configuração; isso não publica o ambiente. Para reutilizar o snapshot pela plataforma, revise/salve as configurações e publique o ambiente na interface.
