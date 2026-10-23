# Validação — RPCore 0.13.0

Build Release net48, SHVDN Enhanced 1.1.0.6/LemonUI SHVDN3 2.2: zero erros e zero avisos. DLL SHA256 `bfd71c15a49f2c9b60564150025bc9769da3d217a3985ccfc3341652aa9ff86b`.

430 testes aprovados, zero falhas/ignorados: 396 anteriores e 34 novos. Cobrem licença/banco/iscas, replay e rollback, bag/capacidade/qualidade, coleta e tempo/overflow/interrupção, três coletas/um término, venda com retirada e crédito no mesmo checkpoint, limites, materiais/unknowns preservados, licença de piloto e empregos anteriores, cancelamento/perda de animal e exclusão entre nove atividades. Relatórios DataTests.trx/DataTests-0.13.0.trx. Não simulam nativos/IA de caça.

O campo Hunting é aditivo no Wallet.json, com licença, iscas, bolsa, contadores e recibos. Material e banco são gravados no mesmo checkpoint; em falha, as referências/banco anteriores são restaurados. A bolsa é exibida no menu de caça; Inventory.json/itens pessoais/porta-malas continuam iguais. Dados desconhecidos são preservados; semanticamente inválidos não são silenciosamente zerados. Animais não coletados/sessão ativa não retomam após reload; coletas/iscas restantes/licença persistem por perfil.

Auditoria PSRP-SYSTEMS-AUDIT-0.13.0.json: binário atual/baseline 0.12.2 e 75 fontes de referência. Açougue coincide com NewInventorySystem.cs; área de floresta é própria RPCore, não atribuída à PSRP. Qualidade/coleta/iscas comparadas estaticamente com _Activity__Hunting.cs. Os oito materiais e três espécies deste ciclo não representam toda a fauna/catálogo da referência. Sem transplante de fonte ou edição de OIV/YTD/RPF/DLC/dlclist.

Inspeção nativa: cria apenas um animal próprio por isca ativa, com ponto livre/chão/água verificados, e só consome isca após spawn/checkpoint. Atração/fuga por tarefas nesses animais, sem alterar IA global. Coleta exige carcaça morta, faca, proximidade e tempo; animação é parada apenas no ped/clipe iniciados pelo RPCore. Kit licenciado dá faca/mosquete ausentes e mínimo de 20 munições no mosquete, sem retirar outras armas. Prop da isca é cosmético com falha isolada do ciclo. Encerramento limpa apenas recursos próprios. Não remove procurado nem fauna/NPCs de ambiente.

O usuário aprovou piloto e teleporte da 0.12.2. Conforme solicitado, refinamentos dos trabalhos ficam após as fases principais. Esta 0.13.0 não foi executada no GTA nesta nuvem: precisam de teste as posições, modelos/atração/fuga, tiro/coleta/qualidade, animação, licença/bolsa/venda e persistência. Roteiro em FASE-5.1-0.13.0.md. Pesca/crafting e demais atividades da fase 5 continuam pendentes.

Instalador: duas execuções em diretório GTA simulado via PowerShell 7/Linux; 93 arquivos existentes preservados byte a byte, incluindo saves, configurações e F2. Hunting.json instalado na primeira execução e personalizações preservadas na segunda. DLL anterior em backup e nova DLL idêntica ao build. Relatório installer-validation-0.13.0.json; não representa execução do GTA.
