# Validação — RPCore 0.5.1

Build Release net48 com SHVDN Enhanced 1.1.0.6 e LemonUI SHVDN3 2.2: zero erros, zero avisos. DLL SHA256 `cbccefe84e8ef4732f384aff8d13fea494d69174b578a806cf8760f21454f0c9`.

112 testes passam, zero falhas/ignorados. Os 16 novos casos cobrem metadados embutidos sem sidecars, merge seguro, ciclo de banners e classificação/JSON do diagnóstico. Os 96 casos anteriores continuam passando. Relatório DataTests.trx. Catalog.json e Bindings.json foram confirmados como recursos do binário Release, sem bytes YTD embutidos.

Varredura estática: 75 arquivos C# exportados da PSRP.dll, 251 construções ScaledTexture, 21 TextureAsset, 2 DRAW_SPRITE e 82 referências de fábrica de slots. Relatório PSRP-TEXTURE-AUDIT.json, hashes dos YTDs em PSRP-YTD-FILES.json, interpretação em AUDITORIA-YTD-0.5.1.md. Expressões dinâmicas/execução não são resolvidas inteiramente; a PSRP.dll não foi executada.

Nenhuma mudança em saves, recursos YTD/RPF, DLC, update.rpf ou dlclist.xml. O caminho esperado foi confirmado pelo assembly.xml do OIV e pelo print do OpenIV do usuário. O novo diagnóstico nativo só pode ser executado no GTA do usuário; não se afirma que ele já retornou 679 matches ou que o visual já esteja idêntico.

Instalador executado duas vezes com PowerShell 7/Linux sobre pasta GTA simulada com a distribuição 0.5.0: 86 arquivos existentes preservados byte a byte, incluindo catálogo parcial e Bindings.json inválido, saves, INIs, carteira e inventário. DLL anterior em backup e DLL nova idêntica ao build. Relatório installer-validation-0.5.1.json. Isto valida o instalador; não executa o GTA nem consultas nativas.
