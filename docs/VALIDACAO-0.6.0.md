# Validação — RPCore 0.6.0

Build Release net48 com SHVDN Enhanced 1.1.0.6 e LemonUI SHVDN3 2.2: zero erros, zero avisos. DLL SHA256 `8b17878f212a52fafd2f0b40da3d2b1c888e7c4e0c6152c6daa6e39008b68b89`.

124 testes aprovados, zero falhas/ignorados. Os 12 novos casos cobrem os três pontos públicos da referência, atualização 3→20, preservação de coordenadas/nomes personalizados, lista vazia, duplicatas, entradas nulas, idempotência, ausência de mutação da configuração e consistência JSON/defaults. Relatório DataTests.trx. Os 112 testes anteriores continuam passando.

Auditoria estática dos tipos de RPCore.dll 0.6.0 e PSRP.dll, com hash do RPCore.dll 0.5.1 distribuído como baseline. Índice dos 75 arquivos de referência, métodos, chamadas de Main e linhas de pagamento: PSRP-SYSTEMS-AUDIT-0.6.0.json. A DLL PSRP não foi executada, e estes índices não comprovam sucesso de gameplay. Comparação de sistemas e aceite por fase em FASES-0.6.0.md.

O usuário confirmou a 0.5.1 funcionando no GTA antes desta alteração. A 0.6.0 ainda não foi testada no GTA. Precisam de teste: acesso aos círculos de lojas com os interiores instalados, compra sem entrar atrás do caixa, ícones/minimapa/GPS, depósito, turno/pagamento de pizza, recarga de scripts e preservação dos saves reais.

Sem alteração em YTD/OIV/RPF, DLC, dlclist.xml, saves ou configurações locais. A compatibilidade com cada recurso instalado só pode ser confirmada no jogo do usuário.

Os 20 pontos de config/Shops.json foram comparados automaticamente, em ordem, com a lista da PSRP exportada; todos coincidem (shop_reference no relatório).

Instalador executado duas vezes com PowerShell 7/Linux sobre pasta GTA simulada com distribuição 0.5.1: 86 arquivos existentes preservados byte a byte, incluindo Shops.json antigo, catálogo parcial, Bindings.json inválido, saves, INIs, carteira e inventário. DLL anterior em backup e DLL nova idêntica ao build. Relatório installer-validation-0.6.0.json. Esta simulação não executa o GTA nem APIs nativas.
