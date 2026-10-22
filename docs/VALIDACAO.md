# Validação da versão 0.2.0 — 22/10/2026

A base anterior, commit `68191401248ec0282dd0d6c50ad96c07625f8790`, foi testada no GTA pelo usuário, que informou funcionamento. Este relato não valida automaticamente os sistemas novos. A validação da base permanece em `VALIDACAO-BASE.md`.

| Verificação atual | Resultado |
|---|---|
| `bash scripts/setup.sh` | Passou; SDK 8.0.419 e referências oficiais Enhanced 1.1.0.6 / LemonUI 2.2 com SHA-256, restores por lockfiles |
| Build Release .NET Framework 4.8 | Passou; zero erros e zero avisos; DLL 0.2.0.0 |
| Testes de dados | **50 passaram, 0 falharam, 0 ignorados**, incluindo 31 regressões anteriores e 19 casos novos |
| Saves PSRP reais | Três fixtures mantidas, com cópia byte a byte dos 151 campos e importação dos campos suportados |
| Combustível | Consumo por distância/marcha lenta, limite de dt, tanque vazio, rejeição de valores inválidos e cotação limitada por saldo/espaço |
| Garagens/dados de veículo | Vagas/posse, bloqueio de identidade duplicada, transferência de garagem e snapshot com mods/danos/campos desconhecidos no round-trip |
| Save da carteira anterior | Carrega sem Transport; preserva dinheiro/banco/campos desconhecidos ao acrescentar dados novos |
| Instalador de atualização | Duas execuções reais via PowerShell 7.6.7/Linux sobre uma pasta temporária com o payload anterior e saves locais: **76 arquivos anteriores preservados**, novos configs instalados, backups verificados |
| Cópia duplicada da DLL | Recusada antes de novos backups/alterações |
| Originais PSRP | Os 62 arquivos de assets permanecem sem alterações e são conferidos no pacote |
| Pacotes | CRC e SHA-256 verificados; DLL do ZIP deve corresponder ao build; ZIP de fontes exclui downloads/bin/obj |

O runner está em `DataTests.trx`; evidência do instalador em `installer-validation-0.2.0.json`. Os testes usam os arquivos reais `Persistence.cs`, `Economy.cs` e `VehicleData.cs` utilizados na DLL. Não substituem natives do GTA por stubs para alegar teste de gameplay.

SHA-256 da DLL: `2cf2bb9d6e946040cf73c6e0db9b288df4a33887439259cfd31087c162b858a4`.

**Aguardando teste do usuário no GTA/Windows:** motor, cruise e cancelamentos, setas/limpeza de estado, posto/consumo/HUD, guardar/retirar/recuperar veículos, visual de customizações/danos, serviços, compra no PDM, distâncias/saídas e coexistência com mods. Roteiro: `TESTES-0.2.0.md`.

Ainda não foram concluídos os demais sistemas amplos PSRP (casas, telefone, polícia, empregos adicionais, seguro, territórios/gangues avançados e empresa PDM). A compra pessoal de veículos desta versão não implementa a empresa PDM.

As instruções cloud `install_script` e `start_skill` continuam salvas como rascunho, sem publicação automática. A atualização do repositório também não publica um snapshot cloud.
