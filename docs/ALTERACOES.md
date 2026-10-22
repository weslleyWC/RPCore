# Análise dos arquivos e alterações

## Entradas examinadas antes da implementação

| Entrada | Conteúdo observado |
|---|---|
| `RPCore.dll` (24.064 bytes) | DLL .NET inspecionada por ILSpy; necessidades, inventário, lojas, pizza, configuração, persistência e utilitários. Sem conquistas, cinto, perfis ou sistemas do PSRP. |
| `rbcorefonte.zip` | Nove arquivos: projeto, Main, Cfg, Store, Prompt, NeedsSystem, InventorySystem, ShopSystem e PizzaJob. O Main já referenciava os complementos ausentes desse ZIP. |
| `ultimacoisa.zip` | Quatro complementos: Profile, Sfx, AchievementSystem e SeatbeltSystem. |
| `PSRP.zip` | 79 entradas incluindo diretórios; recursos, WAVs, JSON, XML, INI e três saves de jogador. Não há fonte C#, projeto ou DLL do PSRP. |
| Prints | Estrutura de pastas/arquivos, sem telas de gameplay ou especificações de comportamento. As pastas foram incorporadas ao pacote com funcionalidades RPCore próprias. |

`input-inspection.json` registra tamanho, SHA-256, esquema dos JSONs e formato dos WAVs examinados. Os fontes enviados são a base da implementação; a DLL foi usada para confirmar a diferença entre a versão compilada anterior e os complementos, sem substituir os fontes por descompilação.

## Correções na base

- Integração dos 13 arquivos C# fornecidos em um único projeto compilável; correção de `Model.IsQuadbike` para `Model.IsQuadBike`.
- Referências das versões exatas informadas: Enhanced 1.1.0.6 e LemonUI 2.2; .NET Framework 4.8, Newtonsoft.Json 13.0.3 e lockfiles.
- Cinto restaura o valor anterior do ped config flag 32 ao retirar, morrer, trocar de veículo/personagem ou abortar o script. Não força o flag do jogador desnecessariamente quando o cinto está desligado. Motocicletas, bicicletas, barcos, aeronaves, quadriciclos e trens são excluídos.
- Troca de perfil salva o perfil anterior e recarrega os sistemas. Encerrar uma pizza durante a recarga não grava as estatísticas antigas no perfil novo. Veículo criado pelo trabalho é liberado no encerramento/abort.
- Entrega tenta encontrar uma rua válida e cancela com aviso se não conseguir, em vez de aceitar um destino aleatório sem rua.
- Fome/sede não drenam durante pausa/cutscene; limites são normalizados, e a fome não reduz a vida abaixo do limite seguro adotado. Configurações numéricas inválidas recebem limites/defaults.
- Preservação de itens sem definição; tratamento de catálogo duplicado; conteúdo desconhecido dos saves de dados é retido por extension data. Dados JSON ilegíveis não são sobrescritos por defaults da sessão.
- Gravação com arquivo temporário no mesmo diretório, substituição atômica e backup anterior. Definições inválidas não são substituídas automaticamente. Migração dos saves RPCore planos é registrada e não repetida em outro protagonista.
- Configurações existentes não são regravadas em toda inicialização; Keys.ini fornecido tem precedência nas teclas de inventário e cinto.
- Teclas repetidas enquanto pressionadas não disparam ações repetidamente. Menus adicionais podem ser fechados pela própria tecla. Erros de UI são registrados; falhas de sistemas têm isolamento e limpeza de recursos.
- Atualização das chamadas obsoletas de notificações e marcadores para a API Enhanced usada; compilação final sem avisos.

## Implementações novas

- `Persistence.cs`: proteção de saves, cópia/importação de jogadores PSRP, retenção integral do arquivo original e dados extensíveis.
- `StorageSystem.cs`: porta-malas destrancado por modelo/placa e baú configurável; depósito/retirada unitária com distância, peso e persistência conjunta com o inventário.
- `Economy.cs`/`Wallet.cs`: carteira para peds MP, banco e operações de bolsa com validação de saldo, quantidades, custo médio e limite de inteiros.
- `StockMarketSystem.cs`: três ativos locais, variação de preço, compra/venda, depósitos/saques, watchlist e preservação de posições sem cotação.
- `GangSystem.cs`: leitura das três gangues, blips/áreas de HQ, GPS, afiliação presencial, saída e reputação por entregas. Nenhum uso dos registros PSRP de veículos, armas, relacionamentos ou territórios.
- `RadioSystem.cs`: catálogo XML com DTD/entidades externas desabilitadas, controle Windows Media Player COM, parada/liberação e detecção de erro/timeout de conexão.
- `CoreMenu.cs`: salvar, banco, recarregar configuração, importar PSRP explicitamente e selecionar o perfil de um ped freemode sem trocar a aparência.
- Conquistas: oito regras RPCore originais e 35 entradas PSRP preservadas no catálogo. Nove entradas PSRP têm gatilhos implementados (início, banco, milhão no banco, pizza, compra, primeiros socorros, armazenamento, cinto e rádio); as demais aparecem como **PSRP: not implemented**, ou como desbloqueadas se importadas assim. Não há desbloqueios falsos para recursos ausentes.
- `Sfx.cs`: associação dos áudios PCM fornecidos aos eventos e descarte dos players ao abortar.
- `config/`: defaults editáveis, três consumíveis adicionais em item pack e baú de exemplo funcional. O instalador não substitui catálogos existentes.
- `scripts/setup.sh`, `scripts/package.py` e instalador PowerShell: build reproduzível, pacote utilizável e instalação com backup/preservação de dados.

## O que permanece fora desta implementação

Os arquivos de dados não fornecem o código nem o comportamento completo de PSRP. Não foram recriados: polícia/LSPDFR, táxi/Uber, emprego de piloto/valet/encomendas, concessionária e funcionários/PDM, compra de casas/garagens, criação de ped/aparência, combustível, manutenção/pneus, drogas, guerras/captura de território, caça, corridas, seguros, roupas, lojas ambulantes, lavagem de dinheiro, telefone, clima real ou streaming Twitch. Os campos, saves, áudios e conquistas correspondentes foram preservados; não devem ser confundidos com sistemas funcionais do RPCore.

Rádio, posição exata dos marcadores, ciclo do cinto e coexistência com mods instalados precisam de teste no GTA/Windows. Não houve execução do jogo neste ambiente Linux. O instalador PowerShell foi revisado, mas a execução dele no Windows também permanece pendente.
