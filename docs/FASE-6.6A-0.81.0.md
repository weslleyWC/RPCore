# 0.81.0 — fiscalização, abordagem e autuações de trânsito

Primeiro pacote funcional de polícia:observação de infrações reais, patrulha observadora de teste, resposta física própria, conferência ao volante, autuação persistente detalhada e pagamento exato pelo banco. Código próprio após comparar BetterPoliceSystem/PrisonSystem daPSRP. **Não implementa prisão/custódia/perseguição criminal ainda.** Transporte prisional como emprego continua separado. Fases8/9 mantidas.

## Instalar

Com o GTA fechado, extraia RPCore-update-0.81.0.zip e execute Install-RPCore.ps1 da pasta extraída com seu caminho real do GTA. Não apague saves/configs nem reinstale OIV/RPF/YTD. Não precisa instalar PSRP.dll junto do RPCore. SHVDNEnhanced1.1.0.6/LemonUI2.2 mantidos.

## Roteiro de teste no GTA

1. **F11 → Polícia / trânsito e multas**. Também disponível no menu centralF10. Ligue **Fiscalização**:é preferência por perfil, começa desligada nos saves antigos. Quem usa outro controlador de abordagens deve escolher se quer ativar também este sistema; não há detecção/integração automáticaLSPDFR.
2. Entre como motorista em carro/moto pessoal comum, fora de missão/trabalho/disputa/alerta/contrato territorial, sem procurado/combate e fora de interior. Viaturas, aeronaves, barcos, bicicletas e veículos reservados de emprego não são fiscalizados por este módulo.
3. Para controle sem multa, use cinto, carro saudável, luzes corretas e respeite o limite. Selecione **Solicitar patrulha observadora (teste)**. Ela surge numa vaga de rua livre e segue por até90s. Um blip azul indica **RPCore:fiscalização de trânsito**. **O botão não inventa infração e não cria multa sozinho.** Sem violação observada, termina sem cobrança. END encerra o teste.
4. Para provocar uma infração simples e verificável, num carro comum retire o cinto pela sua tecla configurada (**Z nos arquivos fornecidos**) e dirija a20–40km/h por uma rua acessível. Aguarde a patrulha se aproximar:ela precisa estar a até50m e enxergar o personagem. Evidência equivalente precisa persistir por2s. Com esses cuidados de velocidade, a infração esperada é **sem cinto:$150**; outras infrações reais podem somar valores.
5. Após o aviso **FISCALIZAÇÃO/encoste**, a autuação foi registrada com placa/rua/itens/valores. **Nenhum dinheiro é retirado nesse momento.** Encoste com segurança e pare ao volante. A viatura precisa parar a até18m, abaixo1,5m/s; seu carro deve ficar abaixo0,5m/s por2s. O agente sai e caminha até o lado do motorista.
6. Permaneça parado ao volante, sem abrir menus, enquanto ele se aproxima a2,5m e confere por7,5s contínuos. Se você mover o carro, o tempo de conferência recomeça. Ao concluir aparece **abordagem concluída**; a viatura/agente próprios são recolhidos em segurança. Não existe congelamento, desembarque forçado ou débito automático.
7. F11 → Polícia/trânsito → autuação → **Pagar pelo banco**. A seleção confirma o débito exato uma vez e marcaPAGA. Exemplo sem cinto:bank−150, dinheiro em mãos não é usado. Saldo bancário insuficiente não tira uma parte nem completa com cash; conserva a dívida. Com abordagem/outra atividade ainda ativa, aguarde o encerramento antes de pagar.
8. Confira após reload/perfil:autuações e pagamentos permanecem, multa paga não cobra novamente. Registro físico aberto durante interrupção/reload muda para **interrompido**, sem recriar policial. Uma infração real já registrada continua pendente para pagamento. Desligar o módulo ou usarEND não apaga dívida.
9. TesteEND antes/depois do aviso, sair do carro, afastar-se300m da viatura, ultrapassar60s de abordagem, trocar perfil/ped, entrar em missão/interior ou receber estrelas do GTA. Encontro é encerrado/interrompido; fugir/expirar registra **não parou/por placa**, conservando somente o valor originalmente observado. **RPCore não adiciona estrelas nem prende nesta entrega**; a polícia nativa continua agindo normalmente sobre crimes do GTA.
10. Veja os documentos **C/M1/A** do DMV no menu. São consulta dos registros existentes; não foi acrescentada multa por licença nem bloqueio de trabalhos antigos.
11. Fiscalização automática ligada procura policial vivo dirigindo viatura policial a50m com linha de visão. Ela apenas lê esse NPC:resposta é feita por um agente/viatura próprios, sem tomar o policial ambiente. Após encerrar, cooldown120s ativos de condições elegíveis ao volante. O teste solicitado manualmente permite dispensar essa espera.
12. Teste as demais infrações conforme sua situação, sem modificar arquivos de saves. Confira F2, teleporteF11, empregos, gangues/emboscada0.80 e comboio0.79 depois. Menus/modal/pausa suspendem relógios de observação/conferência; NPCs não são congelados quando só um menu está aberto.

## Infrações e valores da referência

| Infração | Condição | Valor |
| --- | --- | --- |
| Velocidade | Rua cadastrada e velocidade acima do limite emmph | $150; excesso≥30mph:$250 |
| Cinto | Automóvel,≥10mph e cinto desativado | $150 |
| Faróis | 19h…6h,≥10mph e luzes apagadas | $100 |
| Capacete | Moto sem capacete | $120 |
| Burnout | Nativa informa burnout real | $100 |
| Motor crítico | Saúde do motor≤300 | $75 |
| Pneu | Nativa informa pelo menos um pneu estourado | $100 |

162limites/nomes de ruas conferidos com StreetNames daPSRP, emmph. A tela da infração de velocidade também mostra conversão para km/h. Rua desconhecida não recebe limite inventado nem multa por velocidade, mas pode ter outra infração. Fonte usa2.2369363 como conversão de m/s para mph. Cada tipo aparece uma vez na mesma autuação, com evidência e preço capturados; corrigir depois não altera o que foi observado.

## Comparação e adaptações

BetterPoliceSystem.CheckForViolations/EvaluateViolations:viatura50m/LOS, motorista e regras/valores acima. StartPullOver/HandleEscort/HandlePullOver:ordem de parar, aproximação, saída do agente e inspeção6,5–9s; RPCore adota7,5s. ResolveTrafficStop soma infrações e retira cash nativo comMath.Max(0,cash−total). RPCore substitui essa cobrança parcial automática por dívida identificada e pagamento bancário exato/voluntário, gravado uma única vez na Wallet.

Fonte comanda o policial/viatura ambiente, pode remover armas, força velocidade, oferece suborno, sobeWanted e trata perseguição a pé. RPCore só lê testemunha ambiente e cria resposta própria, exigindo evidência contínua2s antes da autuação, sem assumir o NPC original. Modelo de patrulha/officer próprio configurável. Não porta suborno, telefone, buzina, distância de seguimento ou arma apontada nesta entrega. Não importa painelLSPDFR, não transforma jogador em policial e não forçaWanted.

PrisonSystem da referência encerra re_prison, altera relações/dispatch, zeraMaxWantedLevel e ignora o jogador globalmente. Isso **não foi portado** nem tratado como um ciclo de prisão pronto. Custódia, fichamento, pena/liberação e perseguição ficam para implementação funcional posterior, mantendo compatibilidade. Nenhuma DLL de referência foi executada.

## Persistência e integração

Wallet.Traffic opcional/extensível:Enabled/Pending/Issued/Paid/Collected eTickets até256. Ticket:Id/Street/Plate/Model/LimitMph/Lines(Code/Amount/Value)/Outcome(open,served,evaded,interrupted)/Paid. Começa sem livro/opt-in em saves antigos. Begin valida snapshot e cria pendência somente após resposta própria disponível, sem débito; Finish atualiza resultado da abordagem sem alterar o valor. Pay usa clone e banco/estado de pagamento no mesmo checkpointWallet, rollback síncrono e replay sem nova cobrança. Histórico cheio só remove entrada já paga, jamais descarta dívida; se todas256estiverem em aberto, recusa nova autuação. Lista exibe todas as não pagas e as20pagas mais recentes.

Pagamento não toca cash nativo/MP, veículos, estoque, licenças ou reputações. Campos desconhecidos e livros anteriores conservados. Recarregar registro aberto encerra comointerrupted, sem NPCs duplicados; não há retomada de abordagem nem juros/produçãooffline.

Patrulha usaShiftGate compartilhada, modelos assíncronos10s, nós/chão/espaço de rua eDECORRPCore_TrafficOwner. Veículo reservado emJobVehicles. Cop próprio desarmado/grupo por entidade, sem matriz global de relações. Recolhimento espera ocupantes vivos, movimento/anexos; corpos/carros apenas com corpos próprios são liberados naturalmente. Sem apagar veículo sob o jogador, mexer em entidades ambientes, teleportar, ejetar, encerrar scripts, limpar estrelas/dispatch ou aplicar imunidade. Consultas de cinto/DMV usam os sistemas existentes. Entrada adicionada aoF11 e ao menu centralF10, sem nova tecla nem alteraçãoF2/F11teleporte.

Police/Traffic.json preservado:police3/s_m_y_cop_01, WitnessRadius50(20…80), SampleMilliseconds2000(500…10000), StopSeconds60(30…180), ObserverSeconds90(30…300), CooldownSeconds120(15…1800), InspectionMilliseconds7500(3000…15000), SpeedLimits0…1000ruas com10…200mph. Invalidade mantém arquivo original e usa defaults em memória. Preços das sete infrações seguem a tabela da fonte e ficam capturados nas autuações; mudar limites não reescreve multas anteriores.

## Validação

**3032 testes aprovados**:2992 anteriores+40novos, Release net48 sem erros/avisos. Cobre tabela162/7regras/limites, evidência contínua/perda de visão, snapshot/sem débito automático, resultados/replay/recuperação, pagamento exato/rollback/insuficiência/overflow/capacidade256/metadados e inspeção/parada. Auditoria e hashes emPSRP-SYSTEMS-AUDIT-0.81.0.json. Instalador validado em duas execuções sobre0.80.0.

**GTA não executado na nuvem.** Testar testemunha/LOS, rua/modelos/viatura, observação real, parada/desembarque/caminho/conferência, multas/pausa/encerramento/cleanup e convivência com mods pelo roteiro acima. Perseguição/custódia/prisão e demais fases permanecem pendentes; refinamentos8(interface/necessidades) e9(todos os trabalhos) mantidos.
