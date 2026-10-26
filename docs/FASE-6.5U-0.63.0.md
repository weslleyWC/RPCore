# Fase 6.5U — abastecimento do café e bônus2X — 0.63.0

## Instalação

Baixe RPCore-update-0.63.0.zip da branch rpcore-entrega, extraia e execute o Install-RPCore.ps1 daquela pasta apontando para a pasta real do GTA que contém GTA5.exe. Instalação igual às anteriores; não exige refazer .oiv/.rpf/YTD. ScriptHookVDotNet Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Não instale PSRP.dll junto: referência de análise, não dependência.

## Teste no GTA

1. Com sua gangue controlando **território4**, abra **F4 → Comercio dos territorios**. A seção do território4 tem **Cafe / bonus2X**, **GPS: patio do cafe** e **Abastecer cafe / tres caixas**. Se não controla essa área, use o fluxo de territórios já existente; o contrato não aparece em outras áreas.
2. Use o GPS do café, vá com **seu carro/van**, no banco do motorista e parado a até17m do ponto do pátio. Sem trabalho/combate/procurado/missão/cutscene/modal ou contrato de frete/roubo/venda pendente. Selecione **Abastecer cafe / tres caixas**. Gratuito; não cria mercadoria/dinheiro/bônus ao iniciar.
3. Siga o GPS. Em cada um dos três fornecedores, estacione o mesmo veículo a até35m. A pé, **E** a até3m pega a caixa; **E** a até3m da traseira guarda. Não entre no veículo carregando a caixa na mão. GPS aponta o próximo fornecedor.
4. Com três caixas carregadas, conduza o mesmo veículo até o **pátio do café**, pare a até25m e saia. E na traseira retira uma caixa; leve à **porta do café**, E a até3m entrega. Repita para as três caixas. Mantenha o carro no pátio, a até30m do ponto de estacionamento; não o afaste durante a descarga.
5. Com descarga3/3, **E novamente na porta** confirma a recompensa: **processamento automático2X no território4 por60minutos reais**. Notificação e linha Cafe / bonus2X do F4 confirmam. Feche/reabra menu para atualizar o tempo restante.
6. O bônus não liga o automático. Para comparar, tenha receita suja nessa área e ative **Processamento automatico** a pé no centro do território4 conforme fase6.5T. Feche menus e jogue em segurança com ShiftGate livre/sem contratos. No padrão, cada lote de até$500 passa de60 para aproximadamente**30s ativos**. Um lote inteiro credita$425 no caixa, mantendo15% de taxa. Sem saldo sujo não cria renda. Banco/cash pessoal não muda.

Bônus duplica velocidade, não preço de armas, saldo sujo ou percentual líquido. Processamento manual e venda automática de armas mantêm tempos anteriores. Cada área/gangue/perfil é independente; só território4 ganha esse contrato.

## Pausa, suspensão, retomada e cancelamento

Pausa/menu congelam produção, mas **validade do bônus segue UTC/tempo real**, inclusive GTA fechado, como na referência. Expirado volta à velocidade normal; não produz offline. Avançar horário do GTA não renova. Relógio anterior à concessão não ativa o bônus. Alterar relógio do computador dentro da janela não tem proteção antitrapaça; não há serviço de horário externo.

END suspende o contrato e preserva snapshots. Morte/procurado/combate/perda de posse/veículo/ator/atividade incompatível suspende, sem conceder recompensa. Não apaga/repara/move/reemplaca seu carro.

**Parcial:** volte ao pátio no seu carro/van parado e retome no F4. Refaz as três caixas, pois handles/props não são salvos. Opções, destino e identidade do contrato permanecem. **Pronto3/3:** retome a pé na porta do café, sem exigir carro, e E confirma; nenhum segundo trajeto para carga já entregue.

F4 mostra suspensão/retomada/cancelamento no mesmo controle de frete existente. Cancelar custa/estorna$0, funciona mesmo sem filiação/posse em segurança e não dá bônus. Bônus anterior conquistado permanece com vencimento original. Uma nova rota completa renova para60min a partir da nova confirmação, **não soma duração restante**. Repetir E/recibo não renova nem repaga.

Teste também END/reload na primeira coleta, reload com descarga3/3, cancelamento depois de perder posse, menu aberto durante bônus, processamento manual/auto armas e reinício após bônus expirar. Recursos visuais nativos, carregamento da caixa e acesso do pátio/porta dependem do GTA instalado e precisam de teste real.

## Implementação e conservação

Novo GangCafeData.cs: opções/conta/livro/transações, Wallet.GangCafe opcional por Gang/Area. Contadores Completed e UTC Started/Expires; até640 contas/256 recibos, dados futuros preservados. Consulta e saves legados não criam/ativam bônus. Datas inválidas/livros inválidos, posse/filição erradas, overflow e conflito de contrato recusam sem sobrescrever.

Reutiliza GangSupply: CargoKind0 legal legado,1 criminal e novo2 café. Contrato grátis validado com Cost0/Units3/Capacity3/Area4 e snapshot Cafe + rota. Coletas distintas e descargas sequenciais continuam transacionais. Finalização genérica de mercadoria recusa café; GangCafe.Complete publica **Business sem pendência + Cafe com bônus numa única gravação Wallet**. Falha restaura ambas as referências e mantém pronto; replay não estende. Sem alteração em banco/cash/estoque/receita suja/tesouraria/reputação/diplomacia ao concluir.

GangAutoArmsClock preserva assinatura antiga e acrescenta overload de multiplicador1/2, usado exclusivamente pelo processamento automático. Mantém lotes,85% cumulativos/15%, transação de três livros e travas anteriores. Bônus só acelera tempo de trabalho ativo; pausa/gaps>2s não contam. Perfil/ator/reload zeram intervalo, validade UTC persiste. Livro de bônus inválido protege menu/processamento automático; nenhum reset silencioso.

Novo Gangs/Cafe.json: BoostMinutes60 (1…180), Parking/Door com distância máxima20m e Route de três fornecedores/tempo de rota válido. Config inválida usa padrões em memória e conserva arquivo. Café tem rota independente do frete legal, mesmo compartilhando fornecedores padrão. Alterar config só afeta próximos contratos, não snapshots existentes.

## Comparação PSRP e limites desta entrega

Análise de PSRP/GangSpawner.cs confirma StartCafeSupplyRun restrito a área4, CafeSupplyGoal3, três coordenadas/rumos de coleta, pátio(-1270.415,-1211.753,4.90133), porta(-1275.385,-1200.205,5.370624), multiplicador2 e vencimento DateTime.UtcNow.AddHours(1). Código RPCore próprio; PSRP.dll não executada.

Diferenças: usa carro/van do jogador e checkpoints de carga com caixa carregada na mão; não fornece Burrito nem mantém três caixas visíveis presas dentro da van. GPS sequencial; E final salvo após terceira descarga. Não grava MissionWin em GangState separado da Wallet. Produção opt-in/ativa e tesouraria85% do RPCore, em vez de produçãoUTC/CleanCash da PSRP. Veículo fornecido/props de carga/diálogo/demais trabalhos com bônus/guerras/polícia/refinamento continuam futuros. Fases8(interface/necessidades) e9(realismo dos trabalhos) permanecem reservadas.

2270 testes aprovados (2236 anteriores +34 café/tempo), build Release sem erros/avisos. Auditoria e relatórios de dados/instalador acompanham o pacote. **GTA não executado na nuvem.**
