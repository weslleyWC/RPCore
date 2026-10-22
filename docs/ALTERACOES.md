# Alterações — RPCore

## 0.9.0 — fase 4.1: reposição de lojas

Contrato com Mule2, carregamento obrigatório, cinco lojas por turno, caixa na mão, crédito por descarga e bônus único ao devolver. Prioridade de E só na loja de destino; progresso e configurações preservados. Celular passa a F2 por padrão e respeita a configuração local; garagem usa O em memória quando houver conflito. 30 testes novos, total 256, build zero erros/avisos. Uber/valet da fase anterior confirmados pelo usuário no GTA. [Arquivos alterados, limites e teste](FASE-4.1-0.9.0.md).

## 0.8.0 — fase 3: Uber e valet

Uber com solicitação/tarifa, NPC em calçada, embarque/desembarque real, gorjeta e média de avaliações. Valet com cliente chegando de carro, estacionamento/trava, solicitação de devolução, recuperação da mesma instância e entrega ao dono. Turnos compartilhados com pizza/Post OP; crédito bancário, recibo e avaliação no mesmo checkpoint. Telefone funcional no carro parado como motorista, aplicativo Uber com estado/ações e histórico local; central/GPS inclui os quatro trabalhos. Banners originais BoltBanner/Valet e gatilhos reais de conquistas. Defaults de jobs embutidos e JSONs locais preservados. Build zero erros/avisos, 226 testes. [Arquivos alterados, limites e teste](FASE-3-0.8.0.md).

## 0.7.0 — fase 2: entregas

Contrato Post OP funcional com van, 40 endereços de referência, cinco paradas padrão, retirada de caixas, entrega a pé, crédito bancário e bônus ao retornar. Pizza ganha contrato presencial, mantendo F6 rápido e progresso anterior. Turnos compartilham exclusão, estados, recibos e checkpoint financeiro; veículos pessoais são preservados. Interface integrada a F10/F11/GPS/celular e aos banners YTD existentes. Proteção de garagem/porta-malas para veículos de trabalho. Gatilho real da conquista Post OP. Build sem erros/avisos; 166 testes. [Arquivos alterados, limites e teste no GTA](FASE-2-0.7.0.md).

## 0.6.0 — fase 1: conveniências e mapa

- ShopData.cs/ShopSystem.cs/config/Shops.json: 20 pontos extraídos da lista de lojas da PSRP; correção dos três pontos antigos, incluindo LTD Davis. Overlay em memória apenas para padrões reconhecidos, sem escrita em configs/saves; compra anterior preservada. Bloqueio de interação quando morto/em missão.
- MapBlips.cs, FuelSystem.cs, GarageSystem.cs, DealershipSystem.cs e GangSystem.cs: nomes RPCore, cores e escalas por categoria; HQs com sprite específico e cores existentes. Cada sistema continua dono apenas dos próprios blips.
- StorageSystem.cs: blips para depósitos configurados válidos, removidos no Dispose. Porta-malas não recebe ícone permanente.
- PizzaJob.cs/SettingsSystem.cs: pizzaria no ponto e ícone nativo usados pela PSRP, marcador e GPS; atalho JobKey/F6 mantém o comportamento anterior. Destinos de entrega usam pacote amarelo com rota.
- Auditoria estática dos dois binários, incluindo o RPCore 0.5.1 distribuído como baseline; índice de 75 arquivos de referência em PSRP-SYSTEMS-AUDIT-0.6.0.json. Script reproduzível audit-psrp-systems.py, comparação e critérios por fase em FASES-0.6.0.md. Referência decompilada não foi transplantada para o projeto.
- Versão 0.6.0 no assembly/menu/log/diagnóstico. 12 testes novos de configuração e migração; total 124 aprovados. Build zero erros/avisos. Instalador validado sobre distribuição 0.5.1 simulada, mantendo arquivos existentes.
- Nenhuma mudança em formatos de save, áudio, YTD/OIV/RPF ou DLC. Novos empregos além da pizza básica não fazem parte desta entrega.

# Análise dos arquivos e alterações — histórico 0.1, 0.2.0 e integração visual 0.3.0

## Integração visual 0.3.0

Entradas adicionais analisadas antes das alterações: PSRP.oiv (8.495.302 bytes) e psrp_props.zip (26.257.461 bytes), preservadas byte a byte. OIV: oito YTDs e 679 texturas; DLC RPF7 OPEN: XMLs, RPF interno, 27 modelos e um YTYP. Todos os 28 recursos RSC7 de props foram descomprimidos para inspeção e tiveram os tamanhos de páginas conferidos. Pré-visualizações foram extraídas para conferir formas, proporções e alpha. A tela preta da moldura PhoneTexture é opaca, portanto o código a desenha antes do wallpaper e dos aplicativos.

Alterações:

- `VisualData.cs`: catálogo, vínculos, porcentagens/escala/safe zone e navegação de grade testáveis sem GTA.
- `Visuals.cs`: streaming de dicionários existentes, desenho por sprite, layout proporcional, anéis de necessidades, banners/ícones LemonUI, opções e restauração/fallback.
- `VisualSystem.cs`: HUD de carteira e veículo com estados reais e diagnóstico de oito dicionários/27 nomes de modelo. CarOilIcon é alerta de saúde do motor, não uma nova simulação de óleo.
- `PropPresentation.cs`: props/animações cosméticos de comida/bebida/telefone com carregamento assíncrono, fallback por modelo e limpeza limitada ao que o sistema iniciou. Consumo não depende do sucesso visual.
- `PhoneSystem.cs`: telefone F1 com frame/wallpaper/ícones originais e atalhos de sistemas reais, seleção por teclado/controle e fechamento em transições. Não implementa chamadas, SMS ou todos os aplicativos desenhados no atlas.
- `Main.cs`, `Cfg.cs`, `CoreMenu.cs`, `AssemblyInfo.cs`: versão 0.3.0.0, registro dos três sistemas visuais (18 no total com features habilitadas), roteamento de teclado do telefone e diagnóstico/releitura da configuração.
- `NeedsSystem.cs`, `FuelSystem.cs`, `VehicleControlSystem.cs`, `SeatbeltSystem.cs`: dados reais ligados ao HUD, fallback da versão anterior e estado de cinto/cruise exposto. Não houve substituição de seus saves.
- Menus de inventário, loja, armazenamento, conquistas, veículo, garagem, concessionária, gangues, rádio, bolsa e configurações registrados para banners. Inventário/loja/armazenamento vinculam ícones por item.
- `PizzaJob.cs`: banner PizzaBanner no cabeçalho, preservando o texto anterior como fallback.
- `InventorySystem.cs`: evento de consumo após aplicar o efeito, usado pelo apresentador cosmético; armazenamento/peso/formato de save mantidos.
- `config/Visuals.ini` e `config/Visuals/*.json`: flags, escala, tecla, catálogo real e vínculos. `Inventory/PSRPConsumables.json`: dez alimentos/bebidas com efeitos e preços próprios.
- `scripts/Prepare-VisualAssets.ps1`: verifica hashes, prepara somente o DLC de props no diretório mods e recusa substituição de pacote diferente; nunca edita update.rpf/dlclist.
- `scripts/package.py`: pacote visual separado, sem duplicar binários visuais no ZIP de fontes; originais disponíveis em `visual-assets` no repositório.
- `tests/VisualDataTests.cs`: 13 casos adicionais, além dos 50 existentes. Evidência: `visual-inspection.json`, `visual-installer-validation.json`, previews e relatório do runner.

A instalação pelo OpenIV, o desenho/animações e o registro real dos modelos ainda exigem seu teste no GTA/Windows. Cinco nomes de recursos não tiveram hash localizado no YTYP e não foram ativados por novas ações. Os recursos restantes do PSRP continuam preservados, sem afirmar que todas as funcionalidades existentes nas imagens já foram implementadas. Detalhes e passos: `VISUAIS-0.3.0.md`.

## Atualização 0.2.0 (após confirmação de teste da base pelo usuário)

A versão anterior continua identificada pelo commit `68191401248ec0282dd0d6c50ad96c07625f8790`. O usuário informou que a testou no GTA e que a base funcionou. A atualização atual mantém os sistemas anteriores e acrescenta:

- `VehicleData.cs`: identidade por modelo/placa, tanque/consumo/cotação, cadastro de veículos e validação de vagas; DTOs extensíveis preservam campos desconhecidos.
- `VehicleControlSystem.cs`: motor, cruise por aceleração, indicadores, pisca-alerta, capô/porta-malas; restauração/liberação dos estados alterados e proteção durante missões.
- `FuelSystem.cs`: 26 postos, consumo por distância/marcha lenta, HUD, falta de combustível, compra limitada por tanque/saldo, suspensão em pausas/missões e exclusões de modelos.
- `VehicleSnapshot.cs`: captura/restauração de pintura, equipamentos e danos suportados; nenhum dado de ped/reboque/handling é aplicado.
- `GarageSystem.cs`: compra de três garagens, vagas por perfil, saída do motorista antes de guardar, gravação antes da remoção, recuperação com busca prévia de veículo existente, reparo e lavagem pagos.
- `DealershipSystem.cs`: compra pessoal de cinco veículos configuráveis no PDM, vaga obrigatória, modelo/área/saldo verificados e placa gerada sem colisão detectada. Não inclui o negócio empresarial PDM do PSRP.
- `WalletData.Transport` / `Wallet.Transport`: dados novos agregados à carteira existente, sem sobrescrever os JSONs originais PSRP. Saves antigos não exigem reinício de progresso.
- `Cfg.cs`, `CoreMenu.cs`, `Main.cs`: opções/teclas, versão no menu/log e registro dos quatro sistemas (15 com todos os recursos habilitados).
- `AchievementSystem.cs`: mais três gatilhos PSRP: primeiro carro, garagem e abastecimento (12 de 35 com regras ativas nesta versão).
- `Sfx.cs`: áudio de abastecimento e parada direcionada do som.
- `AssemblyInfo.cs`: versão da DLL 0.2.0.0.
- `config/Vehicles.ini`, `config/Fuel/Stations.json`, `config/Garages/Locations.json`, `config/Dealership/Vehicles.json`: defaults novos separados dos arquivos já usados.
- `tests/VehicleDataTests.cs`: 19 casos adicionais, além dos 31 anteriores; dados reais PSRP continuam nas regressões.
- `scripts/package.py`: exclui os ZIPs publicados em downloads da cópia de fontes para evitar arquivos recursivos/inchados.

Esta atualização ainda precisa do teste de gameplay descrito em `TESTES-0.2.0.md`. As seções abaixo documentam a base 0.1 e seus limites naquele momento; garagem, combustível e compra pessoal de veículos foram acrescentados acima. Continuam pendentes os demais sistemas amplos PSRP, incluindo casas, empregos adicionais, polícia, captura de territórios, drogas, seguro, telefone, aparência e gestão da empresa PDM.

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


## 0.4.0 — interface dos prints

- Novos RadialSystem.cs, SettingsSystem.cs, InterfaceOptions.cs e config/Interface.ini: radial J com arte original, modal de teclado/controle, oito categorias F11, ajustes persistentes, emotes, pontos/GPS e ações explícitas.
- Main.cs: integração dos dois sistemas e consumo exclusivo de entradas do radial/F11; Cfg.cs e Vehicles.ini: garagem F2, compatibilidade com antigo F11 em memória.
- Visuals.cs/VisualSystem.cs: seis círculos compactos, MoneyUI/BankUI, banner PSRPBANNER2 das configurações e inventário azul. Layout antigo NeedsTop=60 reconhecido como padrão anterior.
- NeedsData.cs/NeedsSystem.cs/Persistence.cs: sono por perfil, descanso, importação Sleep e preservação dos saves antigos/campos desconhecidos.
- GangSystem.cs: cadastro de gangues em arquivo separado, HQ e blips. PhoneSystem.cs: atalho Config abre F11. CoreMenu/AssemblyInfo: versão 0.4.0.
- VisualData.cs/testes: navegação circular, formatação compacta, catálogo real do radial e compatibilidade de Needs; 75 testes passam.
- Instalador, downloads e documentação atualizados. Detalhamento/limites/testes dentro do GTA em INTERFACE-0.4.0.md.


## 0.5.0 — referências do Downloads.zip

- InterfaceData.cs: geometria/margens alpha reais, reconciliação de slots sem mexer nas quantidades, dados do telefone e cálculo de dois operandos. InventoryData.cs extrai ItemDef/InvData para testar os modelos reais, mantendo os mesmos tipos e formatos, acrescentando GridSlots.
- InventoryGridView.cs/InventorySystem.cs/Main.cs: grade 5×5 com painel lateral, uso existente, ordenação, descarte explícito, páginas e entrada exclusiva do modal. Shops/Storage continuam usando as mesmas APIs.
- VisualSystem.cs/Visuals.cs: HUD do carro central inferior, status/dinheiro com posições/margens/cores da referência, ruas/bairro/hora/data, placa somente para ruas cadastradas. FuelSystem.cs reposiciona instrução de abastecimento.
- PhoneSystem.cs: tela inicial da referência com arte original, relógio/dia, onze ícones, notas/calculadora/pesquisa/histórico real/cronômetro e atalhos preservados em Settings. Sem serviço Uber/chamadas/SMS; limites explícitos.
- RadialSystem.cs: tamanho correto de texturas de ícones, fontes/posições das instruções. SettingsSystem.cs: offset/fonte/zona segura e opções de unidades/grade.
- InterfaceOptions.cs, Interface.ini, Visuals.ini e Visuals/SpeedLimits.json: opções novas/defaults com preservação dos arquivos instalados. Main/CoreMenu/AssemblyInfo: versão 0.5.0.
- 21 testes novos, total 96; build sem erros/avisos. Instalador/docs/referências/downloads atualizados. Checklist completo em INTERFACE-0.5.0.md.

- Main/ShopSystem/VehicleControlSystem: estado de modal publicado antes do processamento dos sistemas; evita E abrir loja por polling sob o radial/grade e cancela cruise nos modais novos, preservando o comportamento de menus anteriores.


## 0.5.1 — varredura PSRP.dll e streaming dos YTDs instalados

- scripts/audit-psrp-textures.py e relatórios: varredura estática de referências em todos os 75 arquivos C# decompilados; leitura apenas, sem executar/incorporar o código de referência. Mapa entre engine, dicionários, imagens e destino do OIV.
- VisualAssetDefaults.cs + RPCore.csproj: metadados Catalog/Bindings embutidos, overrides opcionais mesclados sem retirar recursos padrão. JSON inválido preservado e ignorado com log. Nenhuma imagem YTD embutida.
- Visuals.cs: requisição com o mesmo parâmetro da LemonUI 2.2; aviso de timeout baseado em solicitação real; banner F11 cíclico 1/2/3 a cada 5 s e altura 128. InterfaceOptions/Interface.ini habilitam congelar o banner caso desejado.
- VisualSystem/CoreMenu: diagnóstico com oito dicionários e resoluções de todas as texturas, consultas distribuídas por frames e VisualDiagnostics.json. Checa mesmo com UI desligada. Resultado não é prova de igualdade de pixels.
- Main/CoreMenu/AssemblyInfo: versão 0.5.1. Testes/recursos do projeto de testes: 16 casos novos, total 112.
- Configurações, saves e YTD/RPF existentes permanecem sem sobrescrita. Pacotes, instalador e docs atualizados.
