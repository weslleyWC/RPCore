# Validação — RPCore 0.5.0

Build Release net48 com SHVDN Enhanced 1.1.0.6 e LemonUI SHVDN3 2.2: zero erros e zero avisos. DLL SHA256: `9b296dd3e1a3de1ec3473f48c807d45d6f406598fda7cc78047998d4d3457ee6`.

96 testes de dados passam, zero falhas/ignorados. Os 21 casos novos usam os modelos reais InterfaceData/InventoryData: evolução do inventário antigo com preservação de quantidades/compartimentos/campos futuros, reparação/ordenação e múltiplas páginas, geometria 5×5, transformação de margens alpha, recursos originais do celular, cálculo seguro, notas/histórico, unidades e margem dos seis indicadores. Os 75 testes anteriores continuam passando. Relatório: DataTests.trx.

Downloads.zip foi inspecionado como dados, sem executar instruções ou programas do arquivo. As três imagens originais de referência ficam em referencias-0.5.0 com hashes; não são capturas de execução do RPCore. PSRP.dll foi consultada somente para referências de UI; código de gameplay não foi transplantado.

PSRP.oiv mantém SHA256 `5435e920c9bfa84c15b53a8d9206cf8a31c993b5f22d85fae895b71611053990`; psrp_props.zip mantém `e781c8a9039b2289635808f7dc080d3ef189f3dafe2fbd9042453ece817ebf1f`. RPF/YTD originais sem edição. Não alterou update.rpf/dlclist nem recursos de outros mods.

Renderização real, input, props/animações e gameplay aguardam teste no GTA/Windows. O build e os testes não demonstram igualdade visual no jogo. Diferenças de revisão da DLL e dos prints foram documentadas, assim como funções ainda pendentes de Uber/telefonia/SMS, durabilidade, crafting e editor de objetos. Checklist em INTERFACE-0.5.0.md.

O instalador 0.5.0 foi executado duas vezes com PowerShell 7/Linux sobre pasta GTA simulada com a distribuição 0.4.0. Os 85 arquivos existentes foram preservados byte a byte, incluindo INIs, necessidades, carteira/veículos, inventário/baús e notas do telefone. DLL anterior em backup, DLL instalada idêntica ao build, SpeedLimits.json novo instalado. Relatório: installer-validation-0.5.0.json. Não é um teste de GTA/Windows.
