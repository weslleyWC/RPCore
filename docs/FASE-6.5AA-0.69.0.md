# Fase6.5AA — loja temporária Legion — 0.69.0

Instale o ZIP de atualização extraído executando Install-RPCore.ps1 da pasta extraída com -GtaPath apontando ao GTA5.exe real. Requer GTA Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Preserva saves/configs, celularF2, teleportF11 e recursos visuais instalados. PSRP.dll é referência estática; não instalar junto.

## Teste no GTA

1. Sua gangue precisa possuir o território7. F4 → Comercio dos territorios → GPS: loja temporaria Legion. Banca em210.3288,-933.1739,30.69181.
2. A pé a até8m da banca, seguro e sem outra atividade/pendência, selecione Abrir loja /10 vendas. Início gratuito. Aguarde o cliente caminhar ao balcão e aproxime-se a até4,5m.
3. E inicia atendimento. Setas esquerda/direita escolhem a peça pedida; E confirma. Depois escolha o cupom exato mostrado(0/5/10/15%) com as mesmas setas e confirmeE. Seleção errada permite corrigir.
4. Na barra, E para o cursor dentro da faixa verde. Acerto confirma venda e receita suja da área7; erro na barra encerra esse pedido sem renda. Aguarde o próximo cliente.
5. Faça10 vendas corretas; após a saída do último cliente, E na banca confirma bônus2X de processamento por60minutos reais.
6. Confira receita, histórico e validade no F4. A receita não é dinheiro pessoal: requer processamento territorial. O bônus não liga o automático sozinho. Com automático ativo/receita suficiente, padrão de$500 por60s ativos passa a aproximadamente30s; taxa15% e caixa85% permanecem. Bônus usam máximo2X e não se multiplicam; sem renda offline.
7. Teste END/reload no meio do pedido, após vendas e após10 vendas. Retome a pé na banca pelo F4: preserva vendas, receita e mesmo pedido/cupom/alvo; seleção e cursor reiniciam. Cancelar conserva vendas já creditadas e bônus anterior, sem prêmio parcial novo. Teste falha na barra, pausa/menu e afastamento.

## Implementação e limites

GangPopUp.cs implementa runtime, clientes próprios, nove props cosméticos opcionais e atendimento. GangPopUpData.cs implementa catálogo30 preços e52 modelos da referência, opções, checkout testável, histórico e transações. Config Gangs/PopUpShop.json; mudanças afetam novos contratos, preservando snapshot do atual. Config inválida é mantida no disco e usa defaults em memória.

Wallet.GangPopUp é opcional/extensível: Pending Id/Gang/Area7/Options/Sales/Revenue/Customer e Accounts Gang/Area7/Contracts/Sales/Revenue/StartedUtcTicks/ExpiresUtcTicks. Histórico parcial Contracts0 tem janelaUTC0; só a conclusão real concede bônus. Cliente captura Id/Model/Item/Discount/Target; pedido não muda ao recarregar. Até640 contas e256 recibos por livro; campos futuros preservados.

Venda clona os livros PopUp e GangIllicit, adiciona Price*(100-Discount)/100 inteiro a Dirty/Proceeds e grava os dois numa única Wallet, com rollback de ambas referências em falha. Não altera Processed/Net/Fees, banco/cash, tesouraria, estoque, roupas pessoais, reputação ou diplomacia. Mercadorias são carga abstrata do cenário, não retiradas do inventário pessoal. Capacidade financeira de todo o contrato verificada ao começar; proteção contra replay e overflow. Cancelamento remove apenas a pendência. Conclusão remove pendência e concede janelaUTC sem creditar vendas novamente; replay não renova.

Trava global e guardas de todas as operações econômicas territoriais impedem sobreposição. Pending suspensa bloqueia novas operações até concluir/cancelar. Bônus de sete atividades observam mesmoUTC e usam máximo2X, com expiração real inclusive fora do jogo. Relógio anterior à concessão não ativa bônus.

DECOR identifica apenas cliente/props próprios. Modelos assíncronos, sem apagar pedestres/carros instalados para abrir espaço. Props coincidentes instalados são preservados; cosméticos sem colisão. Respeita Visuals.Enabled/UseProps. Cliente caminha do entorno; aproximação tem timeout90s ativos. Procurado/morte/combate/perda de posse/perfil/ator, sair35m ou problema de cliente suspendem sem perder vendas/pedido. Liberação de NPC ocupado não ejeta passageiro; não altera polícia, scripts ou relações globais. Props não são obrigatórios para validação econômica.

PSRP/GangSpawner.cs StartLegionShop/SpawnLegionShopProps/SpawnLegionCustomer/UpdateLegionCheckout/FinishLegionSale/CompleteLegionShop conferidos: área7, pontos, nove props,30 itens/preços,52 modelos,10 vendas, cupons, triângulo0,85/s com tolerância0,09, receita descontada e2XUTC1h. Código próprio; DLL de referência não executada. Diferenças: persistência por venda e pedido, suspensão/retomada, finalE e limpeza protegida; sem MissionWin entre arquivos. HUD nativo simples; fases8(interface/necessidades) e9(realismo dos trabalhos) continuam reservadas.

## Validação

2510 testes aprovados(2468 anteriores +42 novos), build net48 sem erros/avisos. Casos incluem venda atômica/rollback/replay, receita/cupom inteiro, metas/expiração/bonus, cancelamento com receita mantida, snapshots/campos futuros, bloqueios/capacidade/overflow e seleção/barra/pausa. GTA não executado na nuvem: precisa validar props, caminho dos clientes, animação, input e apresentação no jogo com seus recursos instalados.
