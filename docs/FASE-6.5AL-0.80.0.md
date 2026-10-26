# 0.80.0 — emboscadas de carro e conversa presencial com líderes

Dois blocos funcionais juntos: eventos rivais de aproximação/tiroteio/fuga e entrada presencial nos sistemas da gangue pelo líder. Código próprio após comparar GangSpawner e GangCreators da PSRP.dll. Não implementa polícia/prisão nem declara toda a paridade da referência concluída. Fases8/9 continuam reservadas.

## Instalar

Feche o GTA, extraia RPCore-update-0.80.0.zip e execute Install-RPCore.ps1 da pasta extraída com seu caminho real. Não apague SaveData/configurações. Não reinstale OIV/RPF/YTD. TelefoneF2, teleporteF11, trabalhos e comboios anteriores preservados.

## Teste único no GTA

### Conversa com líderes

1. F4 → Presença nas HQs:ligada. Use GPS da HQ e aproxime-se do líder visível. Dê espaço ao ponto de spawn; se estiver em cima dele, afaste-se alguns metros e aguarde o streaming. A conversa usa apenas o líder criado pelo sistema de presença, sem procurar/substituir pedestres ambientes.
2. A pé, sem procurado/combate/missão ou outra atividade, aproxime-se a até1,5m e pressione **E**. Também há **F4 → Conversar com líder próximo**. Não precisa ligar novas guerras para conversar. Mantenha-se a até4m enquanto o menu está aberto; afastamento/morte do líder, presença desligada, troca de perfil/ped/filiação, missão/combate/procurado ou outra atividade encerram a conversa.
3. Sem filiação, **Pedir iniciação** usa o tipo já configurado: **3rd Avenue Hustlas:lealdade**, **Eastside Kings:força**, **Blacktop Locos:direção** nos arquivos fornecidos. Não filia imediatamente. Lealdade:dois pacotes/retorno. Força:desarmado, vida acima100, três desafiantes. Direção:carro do jogador/passageiro/alvo/fuga/retorno. Executam os mesmos ciclos já implementados, sem criar uma segunda versão do trabalho.
4. Com iniciação suspensa dessa gangue, a conversa oferece **Retomar**; se estiver pronta, retome e confirmeE no líder conforme o ciclo existente. Uma pendência de outra gangue bloqueia novo pedido. Os atalhos anteriores do F4 continuam disponíveis. Falar com líder rival enquanto já está afiliado não abre recrutamento.
5. Com sua própria gangue, o líder oferece os menus reais de território, assalto organizado, negócios, tesouraria, relações e guerras. As condições de cada operação continuam:abrir um menu pelo líder não libera dinheiro, estoque, posse ou compras. Operações administrativas que exigem centro da HQ podem pedir que você se aproxime desse ponto.
6. **Sair da gangue — confirmar** é explícito:como o Leave anterior do RPCore, remove filiação e zera reputação/patente atual. Não apaga donos de áreas, saldos, estoque ou históricos. Só fica disponível sem disputas/iniciações/contratos/alertas/emboscada pendentes. Teste a saída apenas se desejar essa mudança no perfil; fechar a conversa conserva tudo.

### Emboscadas rivais de carro

7. Para ter um rival elegível, use a declaração de guerra0.78 em F4 → Relações/acordos na sua HQ, ou uma relaçãoHostilidade/Guerra já existente por roubo. Respeito e Neutro não atacam nesse sistema.
8. F4 → **Emboscadas rivais de carro** → ligue **Emboscadas**. A opção começa desligada nos saves antigos, independente das três opções de guerras anteriores. Leia o aviso:é um ataque real com risco de morte; não dá salário ou reputação.
9. Saia da HQ e dos territórios:precisa estar além de40m de todas as HQs e fora do raio configurado das15áreas (45m por padrão). Fique numa rua acessível, fora de interior/água/voo, sem procurado e sem atividade/pendência. Pode estar a pé ou em carro terrestre. Use F11 para agilizar o deslocamento do teste, se desejar.
10. Selecione **Solicitar emboscada agora (teste)**. Não espera o intervalo, mas exige a opção ligada, um rival hostil com modelos disponíveis e vaga de rua livre. Um carro aparece a75–170m, preferindo fora da visão, com motorista e três atiradores. Blip vermelho identifica **RPCore:emboscada[rival]**. Se não encontrar rua/modelos/assentos, não registra um encontro iniciado.
11. O motorista se aproxima por até45s; quando chega a35m, os passageiros recebem tarefa nativa de atirar no seu personagem, uma janela de7,5s. Depois param de atirar/desarmam e fogem por até20s ou até passar220m de distância. A janela de tiro pode terminar depois do prazo de aproximação se começou perto do fim, como na referência. Sem teleporte de motoristas presos no trânsito.
12. Neutralizar o motorista encerra como repelida; perder todos os atiradores inicia fuga marcada como repelida. Se não forem neutralizados e escaparem, registra fuga. Morte do jogador registra morte. Nada credita dinheiro, reputação, estoque, território ou melhoria diplomática. O próprio GTA ainda aplica suas regras normais de polícia/morte; o RPCore não limpa estrelas ou concede imunidade.
13. **END**, **Encerrar encontro atual** ou desligar a opção interrompem. Entrar em HQ/território/interior/água/voo, iniciar missão/modal, trocar perfil/ped/filiação, perder ator ou haver ocupante/conexão externa também interrompe. Outros trabalhos não começam simultaneamente porque a atividade usa a trava compartilhada. As simulações territoriais anteriores aguardam o fim da pendência.
14. **Menus LemonUI que não pausam o GTA não param o encontro ativo**:o relógio e os NPCs continuam, permitindo abrir o controle para desligar/encerrar. Pausa real do jogo para o relógio. Interfaces modais interrompem. Não há produção nem eventos retroativos ao ficar offline.
15. Save/reload durante o encontro **não recria o ataque**. O registro pendente válido é marcado como interrompido no perfil carregado. Isso preserva histórico e evita reaparecerem inimigos a cada reload. Confira os contadores no menu e que o evento não pagou prêmio. O agendamento automático só conta mundo livre elegível sem menu:5–15min ativos e85% por padrão; novo perfil/sessão reinicia o intervalo.
16. Teste a limpeza:carros/peds próprios vazios/parados são recolhidos; ocupantes vivos precisam sair e corpos são liberados naturalmente. Se você entrar no carro rival, ele não é apagado sob você; pare e saia para permitir o recolhimento. Ocupantes e veículos externos são preservados. Confira trabalhos0.77, guerras0.78 e comboio0.79 depois do encontro.

## Referência e diferenças

GangSpawner.IsRivalDriveByBlocked exclui trabalhos de gangue, disputas, territórios e HQ40m. ScheduleNextRivalDriveBy/UpdateRivalGangDriveBys usa300000–900000ms/85%. TryFindRivalDriveBySpawn sorteia90–140m e aceita rua a75–170m, preferindo fora da visão. StartRivalGangDriveBy cria motorista e até3passageiros comMicroSMG350. UpdateActiveRivalDriveBy:aproximação45000ms, alcance35m, tiro7500ms; BeginRivalDriveByEscape:20000ms/220m. Métodos de direção/tiro usam nativas de condução eTASK_DRIVE_BY. Todos conferidos estaticamente; DLL de referência não executada.

RPCore considera apenasHostilidade/Guerra nas relações direcionadas já salvas; a fonte excluiLike/Neutral e poderia considerarRespect hostil. Não importa catálogo/tuning de carrosPSRP:granger configurável próprio. Não teleporta motorista de volta ao banco durante perseguição, não repõe munição a cada ordem, não usa matriz global hostil e não apaga carros ocupados. Armas/tiros apenas nos atiradores próprios com alvo no personagem capturado; não escolhe NPCs ambientes como objetivos. Nenhuma alteração de armas/vida/posição do jogador ou polícia.

UpdateGangLeaderInteraction usa distância ao quadrado2,25 (1,5m); UpdateGangDialogue fecha além4m, pede iniciação sem filiação, deixa sair da própria gangue e rejeita outra afiliação. SpeakToGangLeader/BeginGangInitiation e enumGangCreators.GangInitiationType0/1/2 roteiam lealdade/força/direção. RPCore lê InitiationType pelo campo de extensão existente, sem reescrever definições; aceita inteiro0–2 ou nomeLoyalty/Strength/Driving, tipo ausente em gangue criada assumelealdade, inválido conserva o valor e indisponibiliza o pedido. Não acrescenta fallback físico de co-líder sem modelo do líder; exige ator de presença disponível. Apresentação emLemonUI/banner existente, com links administrativos próprios; desenho de diálogo inferior/câmera/ergonomia ficam na fase8.

## Persistência e arquitetura

GangState.DriveBys opcional/extensível:Enabled,Pending(Id/Gang/Rival/Options/Models4),Started/Repelled/Escaped/Interrupted/Deaths eHistory32. Defaultsfalse/null; saves antigos não são optados automaticamente. Begin só grava após os atores/vagas/assentos existirem, com snapshot; Finish fecha uma vez, conta resultado e guarda facções capturadas. Replay não repete contador, erros síncronos de save conservam o estado anterior, overflow/dados inválidos protegidos. Não há snapshot de posição/vida do encontro ou retomada de tiro; recuperação dePending válida é interrupção. Não cruza Wallet/finanças. Leitura de tipo e conversa não gravam save; somente ação de saída usa clone/umaGangState e mantém campos desconhecidos. F4/Join/Leave anteriores não foram removidos.

MesmaShiftGate; reserva de veículo emJobVehicles;DECORRPCore_DriveByOwner por execução. Modelos assíncronos10s,20consultas a nós de rua/rumo real, espaço/chão/altitude verificados. Apenas recursos próprios; atiradores são desarmados ao escapar/interromper, semClearAll do jogador. Limpeza adiada ou liberação natural preserva NPCs dentro de carros e veículos ocupados/móveis/anexados. Perfil/ator capturados impedem encerrar dados do perfil anterior no arquivo do novo.

Gangs/DriveBy.json:VehicleModelgranger, MinIntervalSeconds300/Max900(60…3600),Chance85(0…100),ApproachSeconds45(10…120),ShootingMilliseconds7500(1000…15000),EscapeSeconds20(5…60),ShootDistance35(10…60). Arquivo inválido conservado; defaults em memória. Config só afeta novos encontros. Novos campos e o novo arquivo não alteram os contratos/regras/saves anteriores.

## Validação e restante

**2992 testes aprovados**:2931 anteriores+61 novos; Release net48 sem erros/avisos. Cobrem preferência/snapshot/counters/replay/rollback/limites/campos futuros, relógio/etapas/pausa/rollover/envelope/HQ/território, interrupção no reload, recrutamento/tipo/proximidade e saída preservando dados financeiros/territoriais. AuditoriaPSRP-SYSTEMS-AUDIT-0.80.0.json associa a DLL e os métodos/constantes da referência. Instalador validado separadamente em duas execuções sobre0.79.0.

**GTA não executado na nuvem.** Validar estrada/vaga/modelos, nativa de tiros/visada/bancos, perseguição/fuga/casualties/polícia, pausa/modal, encerramento/cleanup, diálogo/proximidade e iniciações pelo líder. Nenhum teste automatizado prova navegação/tiroteio/renderização real.

Fecha os blocos de emboscada rival e conversa funcional do líder deste pacote. Polícia/prisão e fase7 continuam pendentes. Variantes móveis de roubo/reforço motorizado/tuning/IA e demais diferenças de paridade continuam registradas, sem declarar toda aPSRP reproduzida. Fase8:interface/necessidades; fase9:refinamento de todos os trabalhos/atividades.
