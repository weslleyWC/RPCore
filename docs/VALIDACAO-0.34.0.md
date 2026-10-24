# Validação — RPCore 0.34.0

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI 2.2: zero erros/avisos. DLL SHA256 `aeaef1d8d3716ca272969b8f189784bfcb5ceb292143f45fd3285d52db11f9e6`.

1208 testes aprovados, zero falhas/ignorados: 1183 anteriores e 25 novos. Cobrem sete pontos/preço, save antigo sem cobrança/aplicação, banco/registro/modelo, cópia/rollback/replay, prévia sem alteração, saldo exato/insuficiente, serviço gratuito configurado, suporte de olhos/modelos, atualização de último visual, metadados desconhecidos, limites/contadores/recibos e isolamento de perfis. Relatórios DataTests.trx/DataTests-0.34.0.trx. Não executam GTA nem verificam renderização nativa.

Campo aditivo Wallet.Barber: Looks/Services/Spent/Receipts. Serviço opcional padrão $150 do banco RPCore apenas ao confirmar mudança; banco e último visual do modelo gravados juntos. Falha restaura livro e saldo. Sem autoaplicar ao carregar scripts/perfil. Reaplicação manual do visual já confirmado, no local e mesmo modelo, não cobra novamente. Housing/Wardrobe/inventário/PDM e configurações anteriores conservados; novo Jobs/Barber.json.

Sete blips/marcadores nas posições da PSRP. Acesso F10 para conservar E das barbearias nativas. Prévia com captura do cabelo/textura/paleta; olhos só nos freemode com getter válido. Variações validadas contra contagem real antes de aplicar, resultado conferido e restauração tentada em falha. Giro opcional do personagem usa câmera normal e restaura orientação ao sair. Fechar/afastar/abrir modal/missão/procurado/morte/perfil/modelo interrompem sem cobrança; backup só é aplicado ao mesmo ped/modelo. Sem controle/câmera persistentes alterados ou NPCs removidos.

Não usa GET_PED_HAIR_TINT_COLOR como getter de tintura do ped: esse native retorna RGB de um índice de paleta. GET_PED_HEAD_OVERLAY retorna índice e não fornece captura completa de cor/opacidade. Tintura/barba/sobrancelhas/rosto/tatuagens permanecem fora desta subfase; não são zerados para montar preview. Não muda modelo/cabeça/roupa/vida.

Auditoria PSRP-SYSTEMS-AUDIT-0.34.0.json: 75 fontes, baseline 0.33.0, DLL atual; BarberMarkers/BuildBarberMenu/ApplyHair e métodos de cor/barba comparados. Pontos iguais; taxa de serviço, snapshots, cancelamento e suporte limitado de protagonistas são próprios. Não declara editor de personagem completo. Fases 8/9 e diferenças de moradia/PDM mantidas.

GTA não executado nesta nuvem. Roteiro FASE-6.3A-0.34.0.md: GPS/lojas, cortes/texturas/olhos freemode, cancelamento/giro, pagamento/recarga/reaplicação, protagonistas conforme variações disponíveis e regressão de moradia/roupas/itens/trabalhos. Aparência e convivência com a barbearia vanilla precisam de teste real.

Instalador executado duas vezes via PowerShell 7/Linux sobre instalação simulada da 0.33.0: 114 arquivos existentes preservados byte a byte, incluindo propriedade Eclipse, roupas, itens/containers/layout, configurações/F2 e empresa. Barber.json acrescentado só na primeira execução; personalização conservada na segunda. Backup da DLL anterior e DLL instalada idêntica ao build. Relatório installer-validation-0.34.0.json. Não é teste de gameplay.
