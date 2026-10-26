# Fase6.5V — divulgação da loja de tatuagem — 0.64.0

## Instalar e iniciar

Baixe RPCore-update-0.64.0.zip da branch rpcore-entrega, extraia e execute Install-RPCore.ps1 **da pasta extraída**, passando o caminho real do GTA Legacy que contém GTA5.exe. Mesmo procedimento anterior. Requer ScriptHookVDotNet Enhanced1.1.0.6/LemonUI2.2 SHVDN3. Preserva saves/configs/visuais instalados .oiv/.rpf/.ytd, F2 e teleportF11. PSRP.dll é referência de análise, não deve ser instalada junto.

1. Sua gangue precisa controlar **território1**. Abra **F4 → Comercio dos territorios**, seção da área1, **GPS: loja de tatuagem**. Se não controla, use o sistema territorial existente. A missão não aparece em outras áreas.
2. Vá a pé até a loja, a até**5m** do ponto(1320.043,-1647.478,52.14559). Seguro, sem procurado/combate/ragdoll/missão/cutscene/modal, trabalho ativo ou frete/roubo/lote/divulgação pendente.
3. Selecione **Divulgar loja / 5 panfletos**. Gratuito; não muda dinheiro/estoque e não concede bônus ao iniciar.

## Entregar panfletos

A missão procura pedestres civis reais a **50–120m da loja**, inicialmente a pelo menos8m do jogador, e marca o mais próximo com GPS/marcador amarelo. Caminhe pela vizinhança; não espere parado na porta. Busca a cada3s, próximo cliente após2,5s de uma entrega. Se não houver pedestre apto, informa espera; não cria NPC falso para completar.

Aproxime-se a pé a até**2,2m**, pressione **E uma vez** e permaneça perto por**1,5s ativos** enquanto ocorre o gesto de entrega. Pode andar acompanhando o cliente. Se sair de alcance, entrar no carro ou abrir pausa/menu/modal durante entrega, cancela o gesto sem contar panfleto; aproxime-se e E novamente. Se a animação não carregar em10s, não confirma e permite tentar novamente.

Cada cliente só conta uma vez por ativação. Mostra notificação e contador1/5…5/5. Não aceita mortos/animais/combate/ragdoll/veículos/policiais/gangues/NPCs persistentes ou de missão/grupos/seguidores. Modelos de todas as gangues cadastradas são excluídos também quando usam ped civil. Pedestres normais continuam suas tarefas; o mod não os congela, torna persistentes, equipa, remove ou altera relações/vida/dinheiro. Um alvo que sair da faixa ou se tornar inapto é descartado sem contar e outro é procurado. O marcador/GPS móvel pertence ao RPCore.

## Voltar à loja e conferir bônus

Depois dos **cinco pedestres distintos**, volte a pé à loja e pressione **E a até5m**. Só essa confirmação encerra o contrato e salva **processamento automático2X na área1 por60minutos reais**. Linha **Tatuagem / bonus2X** no F4 mostra ativo/minutos e histórico de divulgações concluídas; reabra para atualizar a contagem.

Bônus não liga processamento sozinho. Com receita suja na área1, ative o automático no centro da área conforme6.5T e feche menus. Seguro, com ShiftGate livre e sem contratos: padrão consome até**$500 a cada30s ativos**, creditando**$425** ao caixa para um lote inteiro, taxa15% e arredondamento cumulativo preservados. Sem bônus,60s; sem saldo sujo não gera renda. Banco/cash pessoal não muda.

Não muda tempo/valor de venda automática de armas nem do processamento manual. Café da área4 conserva seu próprio histórico/validade. Multiplicadores usam máximo2, nunca somam para4X. Perfis/gangues/áreas isolados; posse e filiação continuam obrigatórias para produção.

Validade segue **UTC/tempo real**, incluindo pausa/menu/GTA fechado, como na PSRP. Produção só avança no jogo ativo/apto; sem renda offline/catch-up. Vencido retorna à velocidade normal. Nova missão completa renova para60min a partir da confirmação, sem somar tempo restante. Repetir E/recibo não renova nem repaga. Adiantar o relógio do GTA não renova; retroceder UTC antes da concessão não ativa. Relógio do computador não tem verificação externa antitrapaça.

## END, reload, retomada e cancelamento

END suspende. Morte/combate/procurado/ragdoll/missão/cutscene/perda de posse/filiação/ator/perfil/atividade incompatível ou afastar-se além de**SearchRadius+100m** da loja suspende sem conceder recompensa. Entrar no carro perto da missão apenas interrompe entrega/produção de panfletos enquanto permanece nele; sair permite continuar.

F4 mostra **Divulgacao pendente**, **Retomar divulgacao**, GPS da tentativa e cancelamento.

- **Parcial0…4/5:** volte a pé à loja, em segurança/sem outra atividade e com posse. Retomar refaz todos os panfletos, preservando identidade e configurações salvas. Handles de pedestres não persistem; a repetição física evita contar o mesmo morador duas vezes após reload/END.
- **Pronta5/5:** progresso salvo; retome a pé na loja e E confirma sem refazer clientes.
- **Cancelar:** grátis, sem estorno/ganho/bônus; disponível mesmo sem posse/filiação em segurança. Bônus conquistado anteriormente conserva vencimento original. Falha de save mantém a tentativa anterior; não apaga para desbloquear.

Contrato pendente, inclusive suspenso, bloqueia comércio/processamento automático/venda automática territorial e novos fretes/roubos/lotes até concluir/cancelar. Outras atividades podem ser feitas após suspensão se a trava global estiver livre. Este bloqueio evita contratos financeiros sobrepostos; não altera contratos salvos de versões anteriores.

Teste entrega parcial/END/reload, pronta5/5/reload/E final, cancelamento após perder posse, cliente morto/em veículo/saindo da faixa, menus e deslocamento durante gesto, bônus expirando, troca de perfil e coexistência com café/auto armas/manual. Densidade real de pedestres e animações/GPS dependem do GTA e precisam do teste do usuário.

## Arquivos e proteção

Novos **GangPromotionData.cs**, **GangPromotion.cs**, **Gangs/Promotion.json**; Wallet/Economy integram livro opcional **GangPromotion**. Pending contém Id/Gang/Area1/Options snapshot e Customers(comprovantes GUID), sem handles salvos. Até640 contas e256 recibos. Contas guardam Gang/Area1/Completed/StartedUtcTicks/ExpiresUtcTicks e campos futuros.

Begin/Give/Restart/Cancel/Complete validam/duplicam o livro e fazem uma gravação Wallet. Complete remove pendência e adiciona recompensa na mesma cópia; falha restaura referência original, pronto/histórico e saldos. Recibos impedem repetição. Sem alteração de banco/cash/estoque/tesouraria/receita suja/reputação/diplomacia ao entregar ou premiar; não grava MissionWin no GangState separado.

GangBulk.Blocked, já usado como trava territorial, inclui divulgação pendente/inválida. Entradas de lote/armas têm guardas explícitas. OtherFree valida os demais livros sem chamar a trava que inclui a própria divulgação; evita recursão/autobloqueio. Livro inválido é protegido e não substituído. Sem migração forçada/opt-in automático no save legado.

Config: Goal5(1…20), BoostMinutes60(1…180), GiveMilliseconds1500(500…5000), SearchRadius120(60…250), MinDistance50(10…SearchRadius−1), ReturnRadius5(2…12), InteractionDistance2.2(1…4), Shop válido. Arquivo inválido usa padrões em memória e preserva original. Novos valores só afetam próxima tentativa; pendências mantêm snapshot. Mudança de duração não reprecifica histórico/bônus concedido.

## PSRP e diferenças

PSRP/GangSpawner.cs StartTattooPromotion/FindTattooPromotionTarget/GiveTattooPromotionFlyer/CompleteTattooPromotion confirmam área1, cinco clientes, posição da loja, faixa50–120m/inicial8m, alcance2,2m, animação mp_common/givetake1_a, retorno5m e multiplicador2/UTC1h. Código RPCore próprio; DLL referência não executada.

Diferenças: confirma panfleto após1,5s ativos, em vez de contar ao solicitar animação; filtra apenas civis comuns/tipos4/5 e exclui persistentes/mission/grupos/modelos de gangues/ragdoll; usa HUD/marcadores existentes em vez do painel PSRP, sem prop de papel. END/salvar/retomar em vez de G/cancelamento imediato; parcial refaz provas/pronto preserva. Não congela clientes nem aplica MissionWin cruzando arquivos. Produção ativa opt-in/tesouraria85%, em vez de rendaUTC/CleanCash PSRP. Refinamentos visuais/realismo continuam fases8/9; mecânico/guincho/demais trabalhos territoriais/diálogo/guerras/polícia seguem futuros.

**2309 testes aprovados**:2270 anteriores +39 novos de dados, regras de pedestres, relógio e integração. Build Release net48 zero erros/avisos. Auditoria e relatórios acompanham o pacote. **GTA não foi executado na nuvem.**
