# RPCore 0.48.0 — fase 6.5F: mapa e captura de territórios

## Instalação e acesso

Baixe e extraia RPCore-update-0.48.0.zip da branch rpcore-entrega. Abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Mantém GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não reinstala .oiv/.rpf nem executa PSRP.dll junto. Novo Gangs/Territories.json só é instalado quando ausente. Preserva gangues/iniciações/equipe, saves/configurações anteriores, DMV C/M1/A, demais trabalhos, F2 e F11.

**F4 → Territorios / mapa e disputas → Mapa de territorios: LIGADO**. Opcional por perfil; saves antigos começam com mapa desligado e sem disputa automática. Não exige ligar a presença das HQs para ver/disputar territórios. Use Fechar/Backspace para sair do submenu e F4 para voltar ao menu principal.

## Mapa e donos

São **15 territórios**, com IDs, centros e todos os postos de ped da referência. O dono inicial segue GangCreators.EnsureDefaultTurfOwnership:

| Territórios | Dono inicial |
| --- | --- |
| 1–5 | 3rd Avenue Hustlas |
| 6–10 | Eastside Kings |
| 11–15 | Blacktop Locos |

Donos iniciais são resolvidos em memória, sem gravar/migrar o save ao carregar. Depois de capturar, o override salvo daquele território prevalece. O nome/cor dos ícones mostra o dono atual deste perfil. Listas de gangues personalizadas precisam conservar/configurar modelos válidos para o dono adversário; não inventa defensores de outra gangue se esse cadastro faltar.

Cada território tem **GPS** e **Disputar** no submenu. O território já controlado pela sua gangue aparece como Sob controle e não pode ser atacado novamente. Posse pertence ao nome da gangue no perfil: sair/trocar de filiação não reatribui os donos antigos. Outro perfil/personagem tem seu próprio mapa de posse. Não importa automaticamente TurfOwnership da PSRP; os arquivos legados permanecem conservados.

## Como disputar

1. Esteja afiliado a uma gangue existente, com mapa de territórios ligado. Escolha território de outra gangue e use o GPS. Para o primeiro teste, prefira um local aberto fora do aeroporto/áreas restritas.
2. A pé dentro de **45 m do centro**, sem procurado/combate/missão/cutscene/interface modal e sem outra iniciação/disputa/trabalho/exame reservando atividade, selecione **Disputar: Territorio N**.
3. Aguarde a preparação. O sistema carrega modelos do dono rival e procura piso/espaço nos postos originais. Padrão: **3 defensores próprios**, com pistolas/120 munições, 200 de vida e sem armadura/invencibilidade/loot. Não remove NPCs/carros de terceiros para liberar vagas. Só registra a tentativa e inicia o combate depois de todos estarem prontos.
4. Elimine os defensores de ícone vermelho, permanecendo **a pé dentro dos 45 m**. Pode usar suas armas normais; o mod não entrega, remove ou restaura seu equipamento. Não há obrigação de matar pessoalmente cada defensor: mortes dos defensores próprios contam, como na referência. Desaparecimento sem morte confirmada não conta e encerra a disputa.
5. Limite padrão: **10 minutos ativos** por luta. Durante a luta, morte/missão/cutscene, entrar em veículo, trocar ped ou abandonar a área encerram sem mudar o dono. Procurado pode surgir: o mod não altera a polícia nem concede imunidade ou acesso a locais restritos.
6. Vencidos todos, grava **captura pronta**. A pé na área, feche os menus e pressione **E** para confirmar. Só após salvar muda o dono para a sua gangue, incrementa o total de capturas e atualiza nome/cor do ícone. Não concede dinheiro, armas, itens ou reputação automática.

Os defensores recebem tarefa contra o jogador; não há mudança na matriz global de relações ou ataque a entidades nativas escolhido pelo código. Reações de polícia/aliados/terceiros continuam nativas. Seus NPCs e ícones são os únicos retirados ao terminar/suspender. As regras nativas de acesso ao aeroporto/áreas privadas permanecem; GPS não concede autorização de entrada.

## Suspensão, retomada e dados

**END**, **Suspender disputa**, desligar o mapa, trocar perfil ou reload retiram os defensores/rota próprios e liberam a atividade. Donos atuais ficam conservados. Retome pelo submenu, afiliado à gangue da tentativa, a pé dentro da área, com mapa ligado, sem procurado/combate ou atividade concorrente.

Luta parcial prepara **defensores novos e zera as derrotas físicas**, preservando ID/modelos/área/regras e total de tentativas. O relógio reinicia; não restaura vida/armadura. Se todos já tinham sido vencidos, a captura pronta permanece salva: retome na área e confirme E sem novos defensores. Sair da área/morrer/iniciar missão depois de pronta também suspende essa aprovação, em vez de atribuir posse automaticamente.

**Cancelar disputa pendente** descarta somente a tentativa. Falha/cancelamento conservam o dono anterior e não cobram/estornam dinheiro. Se a posse tiver sido alterada externamente desde o início, não sobrescreve o novo dono. Enquanto houver disputa pendente, Join/editor de postos e início das três iniciações ficam protegidos até concluir/cancelar. Leave anterior pode sair da gangue, mas não transfere territórios; uma disputa dessa gangue fica suspensa e pode ser cancelada.

Livro opcional **GangState.json → Turf**: overrides Owners por ID, snapshot da tentativa, índices de derrotas/Ready, tentativas/capturas e recibos limitados. Preserva campos desconhecidos e IDs de posse futuros fora dos 15 atuais. Não grava handles de NPCs. Transações clonam, validam e gravam antes de publicar; falha de save mantém o último estado persistido para retomar. Preferência do mapa fica em TerritoriesEnabled por perfil.

Gangs/Territories.json permite ajustar quantidade de defensores (1–6), raio, tempo e áreas/postos com o jogo fechado. Configuração inválida usa defaults em memória e conserva arquivo. Um posto válido no JSON ainda depende de chão/streaming/espaço no GTA; falta de vagas cancela a preparação sem tentativa nova.

## Comparação e limites desta entrega

Referência: LoadTurfs, CanPlayerCaptureTurf, AreTurfDefendersDead, CaptureTurf e GangCreators.EnsureDefaultTurfOwnership. Auditoria PSRP-SYSTEMS-AUDIT-0.48.0.json confere os 15 centros, todos os postos/rumos e as bandas de donos, com índice das 75 fontes e hashes.

Esta é a base funcional de captura. Usa padrão próprio de três defensores/pistolas, início explícito no F4, limite600s, confirmação E, guarda de atividade e persistência/retomada. Não replica ainda as forças ambientes completas, arsenais por gangue, reforços, comboios, contra-ataques/guerras automáticas, comércio/estoque/renda/lavagem ou câmera de captura. Não modifica a reputação limitada já existente. Diálogo completo e polícia também continuam pendentes. Interface/mecânicas seguem na fase 8; refinamento de todos os trabalhos na fase 9.

## Testes dentro do GTA

- Ligue o mapa e confira 15 nomes/cores/donos. Troque perfil/recarregue: sem ícones acumulados ou captura automática. Confira seus cinco territórios iniciais se filiado a uma gangue padrão.
- Vá a um território rival aberto, inicie e confira três defensores/modelos/postos/combate. Tente iniciar de longe, em carro, com procurado ou durante job/DMV: deve recusar sem criar disputa paralela.
- Vença todos, confira que o dono continua anterior até pressionar E; confirme e recarregue. Só esse território deve mudar, uma vez, sem dinheiro/equipamento/reputação duplicados.
- Suspenda após uma derrota e retome: novos defensores, contagem física zerada e mesmo total de tentativas. Suspenda depois de todos vencidos e confirme após reload sem nova luta.
- Teste morte/abandono/veículo/missão durante luta, sumiço de defensor e cancelamento: dono anterior permanece. Teste terreno ocupado/modelo ausente: não apaga entidades alheias. Confira acesso nativo em áreas restritas separadamente.
- Confira filiação/Leave/Join, lealdade/força/direção/equipe, DMV, seguro, trabalhos, inventário, personagem/roupas, F2 e F11.

Build sem erros/avisos e 1744 testes aprovados. A nuvem não executa GTA; combate, piso/IA/acesso, polícia e convivência com outros mods ainda exigem estes testes reais.
