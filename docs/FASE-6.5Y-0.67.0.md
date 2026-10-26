# Fase6.5Y — transporte de valores Gruppe — 0.67.0

## Instalação e início

Baixe RPCore-update-0.67.0.zip na branch rpcore-entrega, extraia e execute Install-RPCore.ps1 **da pasta extraída**, apontando para a pasta real do GTA Legacy que contém GTA5.exe. Mesmo procedimento anterior; requer SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Preserva saves/configs/visuais instalados .oiv/.rpf/YTD/props, F2 e teleportF11. PSRP.dll é referência de análise, não deve ser instalada junto.

1. Controle **território5** com sua gangue. F4 → Comercio dos territorios → seção da área5 → **GPS: Stockade Gruppe**.
2. Vá a pé a até8m do início(-1330.259,-663.4227,26.0517), seguro, sem procurado/combate/ragdoll/missão/cutscene/modal/atividade ou contrato de frete/roubo/lote/divulgação/oficina/guincho/valores pendente.
3. Selecione **Transportar valores / 5 malotes**. Gratuito, não recebe dinheiro/mercadoria/bônus ao iniciar. Deixe vaga livre e **afaste-se4m** do ponto para o Stockade aparecer com segurança. Se carro/pedestre bloquear a vaga, aguarda sem remover terceiros.
4. Entre no banco do motorista do Stockade fornecido. O GPS aponta o ponto de coleta restante mais próximo do blindado.

## Cinco coletas físicas

Os cinco pontos/rumos são os da PSRP. Um segurança próprio aparece quando você chega perto(a até70m), no local livre. Não são policiais/guardas ambiente: o mod cria seus atores, sem armas/dinheiro e com relação civil apenas nesses NPCs. Se você estiver em cima do ponto, **afaste-se2m** para o segurança aparecer. Deixe o ponto livre de veículos/pedestres; a ajuda informa quando aguarda espaço.

Estacione o mesmo Stockade a até35m da coleta, parado abaixo0,5m/s, e saia. **E a até2m do segurança** inicia uma troca de1,5s ativos. Permaneça perto e em segurança. Sair do alcance/abrir pausa ou menu cancela a transferência sem contar. NPC usa gesto de dar e jogador de receber. Se modelo/animação não carregar, não inventa carga nem avança prova.

Com o malote na mão, vá a pé à **traseira do Stockade**, cerca de3,8m atrás do centro. **E a até2,5m** carrega no carro parado. Só agora salva o índice daquela coleta e conta1/5…5/5; cada ponto conta uma vez. Não entre no veículo com o malote ainda na mão, pois suspende sem carga falsa. Depois de carregar, dirija ao próximo ponto do GPS.

Com props habilitados, usa **prop_money_bag_01** na mão e mantém os malotes visíveis presos no interior do Stockade, com os offsets da PSRP. Respeita **Visuals.Enabled/UseProps**, capturados ao ativar: se desativados, omite os props cosméticos mas exige as mesmas ações, distâncias, troca e comprovantes. Não transforma malotes em itens/dinheiro do inventário pessoal. Perda de prop/carga visual durante execução com props habilitados suspende; retomar refaz cargo físico.

## Entregar o blindado

Após cinco malotes, GPS → **depósito Gruppe**(-680.2075,-615.2822,25.30618). Conduza o **mesmo Stockade**, no volante, até6m do ponto e pare abaixo0,5m/s. Não basta chegar a pé deixando o carro longe.

O recebedor aparece em(-675.5438,-613.2014,25.30774) com acesso livre. Se não aparecer e você estiver sobre o ponto, afaste-se2m. Saia do Stockade e pressione **E a até2m do recebedor**, mantendo o blindado a até10m do depósito/parado/sem outro ocupante. Outra troca de1,5s confirma entrega e salva **Delivered**. Depois, **E novamente no ponto do recebedor** confirma o bônus.

Com contrato confirmado, o recebedor próprio tenta assumir o Stockade e sair dirigindo. Não há ejeção do jogador/remoção de ocupados. Se não conseguir entrar em30s ativos, a liberação usa somente o blindado próprio vazio/parado; ocupado/engatado/movendo fica protegido para limpeza posterior. Essa saída é efeito do contrato concluído e não repete recompensa.

## Recompensa

Concluir libera **processamento automático2X na área5 por60minutos reais**. Linha **Valores / bonus2X** no F4 mostra validade/contagem de contratos; reabra para atualizar.

Os malotes **não creditam banco/cash pessoal, receita suja, estoque ou tesouraria**. O prêmio é somente velocidade do processamento já existente. Não liga automático sozinho: com receita suja e automático ativo na área5, seguro/com trava livre/sem pendências, padrão consome até$500 a cada aproximadamente30s ativos, gerando$425 no caixa num lote inteiro e15% de taxa/arredondamento cumulativo preservados. Sem receita suja não cria renda.

Bônus das áreas1/2/3/4/5 são independentes por perfil/gangue/área e usam máximo2X, nunca multiplicação. Manual e venda automática de armas mantêm tempos/valores. Produção exige posse/filiação/apto; validade realUTC inclui menus/pausa/GTA fechado como referência, mas não produz offline/catch-up. Expirado volta ao normal. Novo contrato realmente concluído renova60min a partir da confirmação sem somar duração; replay/E repetido não renova. Relógio do GTA não renova, UTC anterior à concessão não ativa, sem relógio externo antitrapaça.

## END, reload e cancelamento

END suspende. Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator/atividade incompatível suspendem. Blindado destruído/indisponível/abandonado a mais150m, ligação externa/ocupante inesperado, segurança morto, prop perdido ou entrar no carro com malote na mão também suspendem. Pausa/menu/modal interrompem troca não confirmada; nenhum tempo não apto confirma carga/entrega.

F4 mostra pendência, retomada, GPS e cancelamento:

- **Parcial, inclusive5/5 ainda sem entrega ao recebedor:** saia do Stockade antigo e retome a pé no ponto inicial, seguro/com posse. Refaz todas as coletas/carregamentos; handles/props não são salvos. Conserva identidade/opções/pontos e remove apenas recibos de carga para permitir repetir fisicamente sem contagem falsa.
- **Entregue/Delivered:** prova salva; retome a pé a até2m do ponto do recebedor e confirmeE, **sem recriar Stockade/seguranças/malotes**.
- **Cancelar:** grátis, sem pagamento/estorno/bônus parcial. Funciona sem posse/filiação em segurança e conserva validade de bônus anterior. Falha de save mantém estado anterior.

Pendência, mesmo suspensa, bloqueia economia territorial/automáticos e novos contratos territoriais até concluir/cancelar. Outras atividades podem usar a trava global após suspensão. Não sobrescreve contratos antigos.

Objetos/atores/Stockade têm tokenDECOR próprio e reserva de trabalho. Limpeza de veículo só próprio vazio/parado/sem ligação externa; não ejeta jogador, remove carro pessoal/ambiente ou transfere dinheiro. Fila adia ocupados/movendo/ligados e drena mesmo inativo. Encerramento do script marca/libera objetos ainda protegidos sem remoção forçada. Seguranças de coleta só próprios; animações interrompidas são as iniciadas por esta missão. Nenhuma relação global/polícia/scriptGTA é alterado.

## Checklist no GTA

Teste spawn com jogador afastado/vaga livre; cinco seguranças/gestos; bag na mão e cinco props na traseira/interior; distância/carro parado/coleta duplicada/entrada com bag; depósito/recebedor/saída dirigindo. Teste pausa/afastar-se durante troca, END/reload em carga parcial ou5/5 sem Delivered, Ready entregue/reload sem respawn/E, cancelamento sem posse, troca de perfil, ocupação/ligação externa e carro pessoal intacto. Teste props desativados e convivência com nativos de blindado/tráfego/recursos instalados, outras atividades/bônus/manual/auto armas. **GTA não executado nesta nuvem.**

## Arquivos e comparação PSRP

Novos **GangCashRunData.cs**, **GangCashRun.cs**, **Gangs/CashRun.json**. Wallet.GangCashRun opcional: Pending Id/Gang/Area5/Options/Loaded(indices únicos)/Delivered; contas Completed/StartedUtcTicks/ExpiresUtcTicks, até640 contas/256 recibos/campos futuros. Sem handles/timers/props persistidos. Config BoostMinutes60(1…180), ReturnRadius8(3…15), nomes de modelos, Spawn/Depot/Receiver(este até15m em plano doDepot), Sites5(1…10 pontos distintos). Config inválida usa defaults em memória e protege original; snapshots mantêm opções antigas.

Begin/Load/Deliver/Restart/Cancel/Complete clonam um livro e salvam numa Wallet, rollback/replay/posse/limites/overflow. Loaded único não é prêmio: Delivered exige todos os pontos, final remove pendência+concede bônus atomicamente. Falha entre checkpointDelivered e confirmação conserva prova pronta. Outras rotinas bloqueiam livro pendente/inválido sem recursão em OtherFree. Multiplicadores observados no mesmoUTC e combinados pelo máximo, preservando85%/15%.

PSRP/GangSpawner.cs StartGruppeCashRun/CollectGruppeBag/LoadGruppeBag/HandOverGruppeStockade confirmam área5, cinco pontos e rumos, Stockade/segurança/moneybag, mão direita/traseira3,8m/carga2,5m/offsets na carroceria, recebedor assumindo volante e2XUTC1h. Código próprio; PSRP.dll não executada.

Diferenças: segurança mais próximo é carregado por vez em vez de cinco atores simultâneos; troca ativa1,5s, caminhão perto/parado, prova de motorista na chegada e handover/Delivered maisE final salvo. Parcial refaz props; entregue retoma sem respawn; props respeitam preferências. Recebedor sai dirigindo com liberação natural, em vez de apagar tudo após12s mesmo ocupado. HUD nativo simples e sem MissionWin cruzando arquivos. Medicina/demais trabalhos/diálogo/guerras/polícia/refinamentos e fases8/9 permanecem futuros.

**2428 testes aprovados**:2388 anteriores +40 novos. Build Release net48 zero erros/avisos. Auditoria e relatórios acompanham o pacote; física/props/IA precisam do teste do usuário no jogo.
