# RPCore 0.43.0 — fase 6.5A: presença nas HQs de gangues

## Instalação

Baixe e extraia RPCore-update-0.43.0.zip da branch rpcore-entrega. Abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Compatível com GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não reinstala .oiv/.rpf nem executa PSRP.dll junto. Configuração nova Gangs/Presence.json é acrescentada quando ausente. CustomGangs.json, RPCoreCreated.json, GangState.json e saves/configurações anteriores são conservados pelo instalador; celular F2 e teleporte F11 continuam.

## Usar a presença

**F4 → Presenca nas HQs: DESLIGADA → LIGADA** (ou a tecla de gangues configurada). Opcional e persistente por perfil; saves antigos começam desligados. Mantém filiação/reputação e os botões GPS/Join/Leave anteriores. Não cobra taxa nem concede dinheiro, armas ou reputação.

Escolha **GPS: nome da gangue** e aproxime-se da HQ. O sistema usa os modelos e postos de líder/membros já registrados nos arquivos existentes, sem reescrevê-los. A descrição do GPS indica quantos postos são elegíveis; o menu mostra a contagem atual de NPCs. Nem toda gangue terá todos os postos: posições/modelos inválidos ou ausentes são ignorados.

- Padrão: inicia geração a **80 m**, retira depois de **160 m**, máximo **6 NPCs por HQ e 12 no total**. O líder tem um pequeno ícone próprio; membros ocupam seus postos. As HQs mais próximas recebem os novos slots disponíveis.
- Modelos são carregados de forma assíncrona, um posto de cada vez, com timeout. Confere modelo de ped humano, chão próximo da altura cadastrada, espaço de objetos/mapa e peds/veículos próximos. Posição bloqueada aguarda nova tentativa; ninguém é removido para abrir espaço. Se você estiver em cima do posto, afaste-se pelo menos **4 m**.
- Membros que se afastam retornam a pé ao posto quando não estão em combate/veículo; não há teleporte, congelamento nem invencibilidade. NPCs próprios são desarmados e não deixam armas ao morrer. Não mudam matrizes globais de relação nem NPCs nativos. Usam relação civil individual e podem reagir a eventos do GTA; não são uma escolta de combate nesta fase.
- Combate/procurado impedem novos spawns; os NPCs já presentes não são apagados só por isso. Morte/desaparecimento de um NPC impõe cooldown padrão de **120 s** para o posto. Ao desligar, trocar perfil, recarregar/abortar scripts, iniciar missão/cutscene, morrer ou se afastar, retira somente atores/ícones deste módulo.

Pode ajustar limites/radios/cooldown em Gangs/Presence.json com o jogo fechado e reiniciar scripts. Opções fora dos limites usam defaults em memória e conservam o arquivo. Ausência de chão/modelo ou interiores não carregados pode deixar postos vazios; o código não força acesso a interiores.

## Configurar uma gangue criada no RPCore

1. Use a criação de gangues já existente em **F11 → Gang Creation**, no lugar escolhido para a HQ. Abra F4 e use **Join nome** a pé na HQ. Gangues e postos são definições compartilhadas entre perfis; a preferência ligada/desligada é individual.
2. No F4, **Definir posto do lider aqui** guarda sua posição e direção. Exige filiação à gangue do arquivo RPCoreCreated.json, a pé a até 70 m da HQ, sem combate/procurado/missão/cutscene/interface modal. Não altera gangues de CustomGangs.json.
3. Caminhe alguns metros e use **Adicionar posto de membro aqui**. Separe os postos por pelo menos **2 m**, inclusive do líder. Limite de **20 postos cadastrados**, sujeitos ao limite ativo por HQ. Repita para os demais locais.
4. Se a gangue ainda não tiver modelos válidos, o editor usa g_m_m_oggoon_01 para líder e g_m_y_famca_01 para membros. Modelos já configurados são preservados. Não escolhe seu freemode como NPC.
5. **Remover ultimo posto de membro** desfaz apenas o último posto, conservando líder e demais pontos. Reposicione o líder pelo mesmo botão de definição. Afaste-se pelo menos 4 m do local e aguarde os NPCs aparecerem.

Cada edição clona os dados, valida, grava atomicamente com backup e só então substitui a definição em memória. Guarda os outros registros, modelos, metadados desconhecidos, veículos e relações, embora veículos/relações não sejam ativados nesta subfase. Se o JSON estiver inválido, não grava defaults por cima. Editar os postos limpa e prepara novamente somente a presença própria, sem alterar filiação/reputação.

## Comparação e limites

PSRP GangSpawner: BeginSpawn/ProcessQueue, líder e PedSpawnSpots; UpdateGangHQSpawning usa 80/160 m, Despawn remove os próprios atores. GangCreators tem editor de postos/líder. Main chama GangSpawner.Update. Auditoria está em PSRP-SYSTEMS-AUDIT-0.43.0.json, com índice das 75 fontes e hashes dos binários/configuração.

Esta entrega implementa a presença e o editor mínimo de postos com código próprio. Não declara concluídos territórios, guerras, negócios de gangues, recrutamento/escolta, diálogos/iniciações, carros/cães, salários ou integração policial/LSPDFR. Interface/mecânicas continuam reservadas à fase 8; refinamento de todos os trabalhos à fase 9.

## O que testar no GTA

1. Confira saves antigos com presença desligada, filiação/reputação anteriores e funcionamento do F4. Ligue e use GPS de uma gangue com postos configurados; confira líder e membros sem aparecerem duplicados.
2. Afaste-se além de 160 m e volte até 80 m. Teste desligar/religar, reload e troca de perfil: sem NPCs/ícones próprios acumulados. Confira persistência da preferência por perfil.
3. Crie/afilie uma gangue, marque líder e dois membros em piso aberto, afaste-se dos pontos e confira posições/direção. Tente postos sobrepostos/fora de 70 m e remoção do último. Confira que CustomGangs.json e outras gangues não mudam.
4. Teste um posto ocupado/modelo inválido: deve aguardar/ignorar sem excluir ped/carro alheio ou travar o jogo. Teste retorno a pé e cooldown de morte. Confira que combate/procurado não geram novos membros instantaneamente.
5. Confira regressões: DMV C/M1/A, seguro, personagem/roupas, empregos, inventário, F2 e F11. O teste de instalador preserva arquivos em pasta simulada; não substitui estes testes reais.

Build Release sem avisos/erros e 1575 testes de dados aprovados. Física/chão, modelos, navegação, resposta dos NPCs e coexistência com mods ainda precisam do GTA real.
