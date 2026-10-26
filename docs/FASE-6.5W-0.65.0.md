# Fase6.5W — contratos da oficina territorial — 0.65.0

## Instalar e começar

Baixe RPCore-update-0.65.0.zip da branch rpcore-entrega, extraia e execute Install-RPCore.ps1 **da pasta extraída**, com o caminho real do GTA Legacy que contém GTA5.exe. Mesmo procedimento anterior, SHVDN Enhanced1.1.0.6/LemonUI2.2 SHVDN3. Sem reinstalar .oiv/.rpf/YTD/props; preserva saves/configs/F2/teleportF11. PSRP.dll é referência de análise e não dependência para instalar junto.

1. Sua gangue precisa controlar **território2**. F4 → Comercio dos territorios, seção da área2 → **GPS: oficina territorial**.
2. Vá a pé a até**5m** da oficina(542.1745,-187.7679,54.50751), em segurança, sem procurado/combate/ragdoll/missão/cutscene/modal/atividade ou contrato de frete/roubo/lote/divulgação/oficina pendente.
3. **Deixe a vaga livre** e seu carro estacionado longe dela. Selecione **Atender oficina / 3 carros**. Gratuito; sem dinheiro/estoque/bônus na contratação.

## Reparar os três clientes

O contrato sorteia e salva três modelos do catálogo120 da PSRP. Um carro danificado com motorista próprio nasce na rua ao sul e chega dirigindo à vaga. Aguarde o cliente parar, com espaço para entrar; o jogador não deve dirigir o carro do cliente.

A chegada tem até**90s ativos**. Se a rota estiver bloqueada e o cliente não chegar, suspende conservando reparos anteriores. Vaga/ponto de spawn ocupados aguardam e avisam para desobstruir; o RPCore não remove carros/props de outros mods. O carro chega pelo tráfego/navmesh, sem teleporte para a vaga.

Na frente do carro parado:

1. A pé, aproxime-se a até**2,8m** do ponto frontal, aproximadamente2,5m à frente do centro do veículo.
2. Pressione **E** para abrir o capô. O personagem inicia animação de reparo.
3. A barra mostra um cursor branco oscilando e uma faixa verde central. Pressione **E quando o cursor estiver no verde**. Precisa de**3 acertos** por carro. Erros só aumentam o contador; não cobram dinheiro nem reiniciam acertos anteriores daquele carro.
4. Há proteção contra repetição: primeiro comando após500ms ativos, próximos após400ms. Cada tentativa reinicia a oscilação. Não basta segurar/apertar E continuamente.
5. No terceiro acerto, salva o carro concluído, repara **somente o carro próprio do cliente**, fecha capô e manda o motorista sair. Aguarde a saída; após30m da oficina ou45s ativos, libera o cliente e prepara o próximo. Se o trânsito mantiver um carro na vaga, novos clientes aguardam sem remover o bloqueio.

Repita nos três carros. Depois da saída do último, volte a pé ao ponto da oficina e **E confirma o contrato**. O progresso3/3 já fica salvo antes da recompensa.

## Recompensa e compatibilidade

Confirmação final libera **processamento automático2X na área2 por60minutos reais**. Linha **Oficina / bonus2X** no F4 mostra validade/histórico; reabra o menu para atualizar.

Não liga automático sozinho. Com receita suja e automático ligado na área2, jogando seguro/ativo com trava livre e sem contratos, padrão processa até**$500 a cada aproximadamente30s ativos**, com**$425 ao caixa** num lote inteiro e15% de taxa. Mantém arredondamento cumulativo/tesouraria existentes; sem saldo sujo não cria renda. Não paga banco/cash pessoal nem cria estoque/reputação/diplomacia.

Bônus do café(area4), tatuagem(area1) e oficina(area2) têm livros/validade separados. Usa máximo2X, nunca4X/8X. Manual e venda automática de armas mantêm seus tempos anteriores. Exige posse/filiação/perfil corretos para produzir.

Validade realUTC inclui pausa/menu/GTA fechado, como na PSRP; produção só conta jogo ativo/apto, sem renda offline/catch-up. Expirado volta à velocidade normal. Nova rota de três reparos completa renova60min a partir da nova confirmação, sem somar duração restante. Replay/E repetido não renova. Horário do GTA não renova; UTC anterior à concessão não ativa. Não há relógio externo antitrapaça.

## Suspender, retomar e cancelar

END suspende. Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator/atividade incompatível, afastamento maior150m da oficina, cliente morto/veículo destruído ou tomado pelo jogador suspendem sem recompensa parcial. Pausa/menu/modal congelam temporizadores/minigame e interrompem animação; afastar-se da frente congela a barra/acertos sem consumir tentativas. Animação indisponível tem proteção de timeout e conserva contrato.

F4 mostra **Oficina pendente**, **Retomar oficina**, GPS da tentativa e cancelamento.

- **Parcial0…2/3:** retome a pé na oficina, seguro/com posse e sem outra atividade. Recria apenas o próximo carro com modelo e opções já salvos. Carros concluídos não são repetidos. Acertos/erros/capô do carro atual não persistem e reiniciam; sem handles de entidades nos saves.
- **Pronto3/3:** após END/reload pode retomar a pé na oficina e confirmar E sem esperar/gerar outro cliente.
- **Cancelar:** gratuito e sem prêmio parcial, funciona mesmo sem posse/filiação em segurança. Não elimina bônus anterior conquistado; conserva vencimento original. Falha de save mantém estado anterior.

Contrato pendente, inclusive suspenso, bloqueia novas operações econômicas territoriais e automáticos até concluir/cancelar. Outras atividades podem usar a trava global após suspensão. Nada sobrescreve contratos antigos existentes.

Clientes/veículos têm marca própria e reserva como veículo de trabalho. Limpeza marca somente os próprios para liberação natural do GTA; **não usa Delete, teleporte, reparo ou remoção do carro pessoal/ambiente**. Se o jogador ocupar o carro do cliente, aguarda sair antes de liberar na sessão; não o ejeta. Fila limitada8 e drenada mesmo inativo. No encerramento do script, apenas marca/libera, sem remoção forçada.

## Checklist no GTA

Teste chegada pelo trânsito e acesso à vaga com recursos instalados; abrir capô/fumaça/animação; três acertos e erros/repetiçãoE; saída e próximo cliente; pagamento somente como bônus final. Teste pausa/menu/afastar-se, END após primeiro carro e reload, pronta3/3/reload, cancelamento sem posse e troca de perfil. Teste entrar/sair do carro do cliente e verificar liberação, carro pessoal intacto e convivência com café/tatuagem/auto armas/manual. Recursos nativos/IA/tráfego não foram executados na nuvem.

## Arquivos, transações e referência

Novos **GangMechanicData.cs**, **GangMechanic.cs**, **Gangs/Mechanic.json**. Wallet/Economy integram GangMechanic opcional: Pending Id/Gang/Area2/Options snapshot/Models capturados/Repaired; contas Gang/Area2/Completed/StartedUtcTicks/ExpiresUtcTicks; até640 contas/256 recibos/campos futuros. Legado não ganha livro/ativação ao consultar.

Begin/Repair/Cancel/Complete clonam um livro e persistem numa Wallet. Cada carro usa índice sequencial e recibo, sem duplicar; confirmação final remove pendência+concede bônus no mesmo save, rollback da referência em falha. NativeRepair vem depois do checkpoint do minigame válido; falha técnica conserva carros contabilizados. Não grava MissionWin no GangState separado.

Trava territorial compartilhada inclui mecânico pendente/inválido. Bulk/Arms e OtherFree da divulgação têm guardas explícitas; OtherFree do mecânico valida livros diretamente sem recursão/autobloqueio. Bônus máximo integrado ao relógio automático existente, preserva85%/15% cumulativos/tesouraria.

Config: Goal3(1…10), RequiredHits3(1…10), BoostMinutes60(1…180), ReturnRadius5(2…12), ArrivalSeconds90(30…180), DepartureSeconds45(15…90), DriverModel, Shop/Spawn/Bay/Exit e catálogoModels120(1…160 nomes válidos/únicos). Bay até20m da loja e spawn/saída entre20…500m em plano. Config inválida usa padrões em memória e protege original; alterações só afetam próxima contratação, snapshots e bônus já concedidos permanecem.

PSRP/GangSpawner.cs StartMechanicRepair/SpawnMechanicRepairVehicle/UpdateMechanicSkillMinigame/CompleteMechanicRepairContract confirmam área2, três carros/três acertos,120 modelos, posições, dano450/550/800 e motor180 na vaga, capô/frente2,5m/alcance2,8m, mini@repair/fixing_a_ped, seno3,5/faixa[0,458;0,542]/cooldown400ms e2X/UTC1h. Código próprio; PSRP.dll não executada.

Diferenças: sem teleporte do cliente para a vaga; temporizadores/minigame ativos, confirmaçãoE final após três carros e retomada persistente, liberação marcada sem deletar, HUD nativo simplificado/sem MissionWin cruzando arquivos. Guincho/demais trabalhos/diálogo/guerras/polícia seguem no roteiro. Fases8(interface/necessidades) e9(realismo/refinamento dos trabalhos) permanecem reservadas.

**2348 testes passaram**:2309 anteriores +39 de oficina/skill/integração. Build Release net48 zero erros/avisos; auditoria e relatórios acompanham. **GTA não executado na nuvem.**
