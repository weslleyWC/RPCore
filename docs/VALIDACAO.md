# Validação — RPCore 0.26.0

Build Release net48 com SHVDN Enhanced 1.1.0.6/LemonUI 2.2: zero erros/avisos. DLL SHA256 `3ba9b49d5912c533b27447fcb5a2229a471101d4ad6e05dbe35512a3b8127eec`.

965 testes aprovados, zero falhas/ignorados: 934 anteriores e 31 novos. Cobrem 19 slots/configuração, atribuição/estoque/dinheiro, unicidade e handles, respawn/recolhimento seletivo, integração entrega/reparo/locadora, preço de atendimento com dano/divulgação, pagamento atômico/limites, cancelamento/recuperação/replay/rollback, campos desconhecidos e máquina de estados/tempo/overflow. Relatórios DataTests.trx/DataTests-0.26.0.trx. Não executam GTA.

Campos aditivos Pdm.Showroom e ShowroomSale, vazios por padrão para saves anteriores. Atribuir/recolher não altera caixa nem concede carro. Unidade exposta não pode entrar em contrato concorrente. Preparar entrega/reparo remove só a atribuição selecionada; locadora exclui atribuídos. Venda confirma caixa/receita/custo/estoque/vaga/intenção juntos; preço é snapshot da oferta, sem reescrever após salário vencer. Falhas restauram estado; replay não paga nem remove outro item. Atendimento não pago interrompido cancela intenção preservando estoque/vaga.

Spawn fixo por slot com limites de modelo, obstáculos/chão e interior pronto quando aplicável, sem buscar outra vaga numerada ou apagar ambiente. Novo helper não altera buscas dos trabalhos anteriores. Native correto do Enhanced: GET_INTERIOR_AT_COORDS. Temporários próprios trancados/congelados/protegidos e reservados, com placa/identidade. Afastar recolhe representação segura mantendo metadata; perto recria só se ausente e vaga livre. Entrada/saída bloqueada/interior não pronto conserva ou recusa montagem, sem forçar portas/IPLs.

Comprador próprio em ponto público livre próximo, blip, aproximação a pé/desarmado, gesto de três segundos e venda/saída. Ocupantes/movimento impedem recolher. Depois de pagar, destranca/descongela para comprador; timeout limpa só recursos próprios seguros sem repetir venda. Cancelar/limites antes de pagar conserva carro atribuído. Perfis/contratos/saldos e recursos OpenIV anteriores preservados.

Auditoria PSRP-SYSTEMS-AUDIT-0.26.0.json: 75 fontes indexadas, baseline 0.25.0 e identidade da DLL atual. ShowroomSpots, métodos de spawn/parking/venda e 19 posições/headings conferidos. Atendimento explícito externo é adaptação própria. Sem declarar câmeras/coberturas, venda aleatória por reputação, reformas físicas, acesso forçado a interiores ou toda a PDM concluídos. Fases 8/9 mantidas.

GTA não executado nesta nuvem. Roteiro FASE-6.1H-0.26.0.md: dois carros externos/posições/alturas/placas/preços, persistência/afastamento, vaga ocupada, recolhimento seletivo, comprador/pagamento/saída/cancelamento, interiores conforme instalação e regressão de entrega/reparo/locadora/financiamentos/retomada/coletas/pessoal/garagem/F2/F11.

Instalador executado duas vezes via PowerShell 7/Linux sobre instalação simulada da 0.25.0: 106 arquivos existentes preservados byte a byte, incluindo saves/configurações/F2 e empresa. PdmShowroom.json acrescentado só na primeira execução; personalização conservada na segunda. Backup da DLL anterior e DLL instalada idêntica ao build. Relatório installer-validation-0.26.0.json. Não é teste de gameplay.
