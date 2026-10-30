# Fases 8 e 9 — escopo definido pelo usuário

Após testar a 0.16.0, o usuário confirmou o funcionamento dos trabalhos e pediu reservar duas fases para refinamento profundo. Os ciclos atuais são a base funcional; vários trabalhos ainda estão simples/irreais. Também relatou problemas no funcionamento das mecânicas de fome/sede e demais indicadores, distinguindo isso da imagem dos ícones.

A ordem permanece: concluir as atividades restantes da fase 5 (streamer e arrancada), entregar fases 6 e 7 e então executar **8: interface/mecânicas** e **9: todos os trabalhos**. Este registro não implementa nem declara corrigidos os problemas relatados.

## Fase 8 — interface inteira e mecânicas associadas

Abrange HUD e todos os menus/interfaces existentes ao chegar a esta fase: indicadores de fome, sede e demais estados, F11, radial J, celular F2, inventário, painéis e telas dos sistemas implementados. Não se limita a mudar sprites, cores ou posições.

Para cada indicador, acompanhar o ciclo inteiro: origem do valor, atualização por tempo/estado, ações que consomem ou recuperam, efeitos no personagem, limites, salvamento/carregamento e representação visual. Identificar a causa dos problemas de mecânica relatados antes de corrigir. Uma textura que aparece corretamente não comprova que o sistema funciona.

Para cada menu, conferir navegação, hierarquia, entradas, ações reais, atalhos, modal/pausa, layout e comportamento em diferentes situações e resoluções. Preservar F2 para o celular, teleporte do F11 e personalizações do usuário.

A referência é a PSRP.dll fornecida, cruzada com YTD/OIV/DLC instalados quando necessários à apresentação. Registrar o caminho entre menus, handlers, estados e arquivos usados pela referência e confrontar com o RPCore. Metadados/decompilação são evidência estática; o comportamento final precisa ser validado no GTA.

Critério de entrega: mecânicas e HUD coerentes entre si, ações de menu completas, valores/efeitos/persistência testados e roteiro de verificação no jogo para cada grupo corrigido.

## Fase 9 — refinamento de todos os trabalhos e atividades

Abrange todos os trabalhos implementados até lá, incluindo os atuais de pizza, Post OP, Uber, valet, reposição de lojas, combustível, transporte prisional e piloto; caça, pesca, crafting/feira e tráfico; e os sistemas que forem acrescentados nas fases seguintes. O refinamento não deve ficar restrito a um ou dois trabalhos.

Antes de alterar cada trabalho, estudar a implementação correspondente na PSRP.dll: inicialização/chamadas, menus, requisitos, estados/transições, posições, veículos, NPCs, objetos/animações, interações, progressão, pagamentos, eventos/riscos, falhas e encerramento. Confrontar isso com o ciclo atual do RPCore e registrar as diferenças concretas, inclusive o que ainda não estiver implementado. Não presumir que um método/classe presente na referência equivale a uma função completa ou ligada ao jogo.

Usar essa comparação para implementar, com código próprio, as interações e etapas que tornem o trabalho mais completo e coerente com a referência. Não tratar alterações apenas de pagamento, coordenadas ou prazo como refinamento completo quando faltarem etapas de gameplay. Diferenças deliberadas de arquitetura/compatibilidade devem ser documentadas.

Entregar em subfases por trabalho/grupo, com DLL compilada, testes pertinentes e teste real pelo usuário: começar, cumprir todas as etapas, receber corretamente, repetir, cancelar, falhar, morrer e trocar de perfil. Conferir limpeza somente dos recursos próprios, compatibilidade com mods e continuidade dos saves. Preservar dinheiro, estoque, licenças, reputações e demais progressos existentes.

## Regras comuns

- Varrer a PSRP.dll e os recursos pertinentes antes de implementar os refinamentos; reutilizar a análise já feita e aprofundar os métodos de cada sistema.
- Separar evidência encontrada na referência, comportamento confirmado no GTA e decisão de implementação do RPCore.
- Registrar o que mudou, diferenças restantes e exatamente o que o usuário precisa testar.
- Manter arquitetura, saves, configurações e recursos instalados; não reiniciar o progresso para viabilizar um ajuste.
- O aceite do ciclo básico de uma fase anterior não remove seu trabalho da revisão da fase 9.

## Aceite mais recente

Núcleo PDM 0.19.0 testado/aprovado pelo usuário, com pedido explícito de refinamento posterior. A 0.20.0 acrescenta coleta de um/dois pedidos pagos e uma exposição externa; não conclui toda a empresa nem antecipa fases 8/9. Incluir logística/empresa/cliente no refinamento dos trabalhos da fase 9.

Continuação 0.21.0: gestão de mecânico/divulgação, salários/acerto e melhorias econômicas. Incluir na fase 9 comparação da IA dos funcionários, calendário/dívidas/efeitos e serviços completos; esta subentrega não declara IA física ou paridade empresarial completa. Pedido de avançar após a 0.20.0 não substitui teste detalhado da logística.

Continuação 0.22.0: coleta física de valores da empresa com Stockade, segurança/maleta, reserva/entrega/compensação e recuperação. Incluir chegada/embarque/navegação e diferenças de cofre/seguro/roubo na comparação da fase 9; não se declara a PDM inteira concluída.

Continuação 0.23.0: entrega financiada para clientes adimplentes, entrada/parcelas por tempo ativo e livro de contratos. Inadimplência/retomada não são declaradas implementadas nesta subfase; são a próxima entrega prevista da PDM. Comparar economia/calendário/interações/risco ao aprofundar a fase 9, mantendo a fase 8 para interfaces e mecânicas.

Continuação 0.24.0: risco apenas em novos financiamentos e retomada da variante estacionada, com Packer/TRFlat e retorno do bem ao estoque. Preserva risco zero da 0.23.0. Variantes com devedor dirigindo/confronto, negociação/cobrança e física detalhada de guincho/rampas ficam pendentes para subentregas/refinamento; não são declaradas concluídas. Incluir acesso aos locais, contabilidade de custo/saldo encerrado e danos na fase 9; interfaces na fase 8.

Continuação 0.25.0: fornecimento de dois carros à locadora, carga/entrega/buzina/funcionário/retorno/pagamento e recuperação. Não confundir com aluguel pessoal por diária. Fila aleatória de pedidos, áudio/diálogo e física detalhada continuam diferenças para refinamento; não se declara a PDM inteira concluída. Pedido “continue” durante o trabalho não comprova teste no GTA.

Continuação 0.26.0: showroom com 19 posições de referência, atribuição/temporários/preços, recolhimento seletivo e comprador na entrada externa. Câmeras, coberturas, venda automática por reputação e reformas físicas permanecem diferenças. Validar pisos/acesso por vaga, navegação/saída do comprador e integração de estoque na fase 9; revisão global de interfaces segue na fase 8.

Continuação 0.27.0: mecânico/zeladoria físicos, manutenção solicitada dos carros expostos, sujeira/cotação e salários da zeladoria. Comparação de posições/modelos/animação na PSRP registrada; limpeza de piso/lixo e IA de seguranças não implementadas. Economia/tempos/navegação são adaptações próprias a aprofundar na fase 9; apresentação/mecânicas na fase 8. Aceite do usuário não foi inferido dos testes automatizados.

Continuação 0.28.0: segurança presencial com dois cargos/postos, salários/acerto e defesa local seletiva de NPCs ambientes, com retorno e suspensão por dívida/procurado/missão. Posições/modelos/arma/20m comparados na PSRP; invulnerabilidade e relações globais não são portadas. IA/alcance/retorno/convivência com outros mods precisam de teste e refinamento na fase 9; interface na fase 8. Roubo/seguro/CCTV não implementados; sem declarar PDM completa.

Continuação 0.29.0: compra/equipamentos CCTV com três mounts da PSRP e monitor ao vivo próprio, modal, troca de canais e saída/restauração. Preview de compra da referência não é declarado viewer CCTV pronto. Enquadramentos/renderização/convivência com câmeras de outros mods exigem teste/refinamento; seguros/roubos e outras diferenças da empresa permanecem. Fases 8/9 mantidas.

Continuação 0.30.0: cofre administrativo com abertura voluntária, exposição distante, proteção contratual 30/35/35, perdas/restituição atômicas e seguro com prêmio/renovação/cancelamento. Cofre fechado/sem apólice em saves antigos. Analisados risco e registro de seguros da PSRP, com diferenças de relógio/prêmio/cap registradas. Não implementa prop/animação de cofre, ladrão físico ou seguro pessoal. Refinar interface na fase 8 e economia/comportamento na fase 9; sem declarar PDM inteira concluída.

Continuação 0.31.0: fase 6.2A de moradia, aquisição/acesso Eclipse por perfil e verificação de interior/piso. Classe Apartments comparada; ausência de chamada externa na exportação registrada. Guardarroupa/storage/hotéis e outros imóveis permanecem; diferenças PDM não são apagadas ao avançar moradia. Fases 8/9 preservadas.

Continuação 0.32.0: fase 6.2B com biblioteca por perfil/modelo e editor reversível no guardarroupa Eclipse. Métodos de outfits e ponto comparados na PSRP; sem auto-load/alteração de cabeça/cabelo/modelo. Validar roupa/texture/props/paleta/cancelamento no GTA; apresentação na fase 8. Storage/editor facial/hotéis e diferenças anteriores permanecem.

Continuação 0.33.0: fase 6.2C com armazenamento privado Eclipse no Inventory.json existente e correção de rollback na transferência de itens, incluindo depósitos/porta-malas antigos. Manager/transferência de unidade/pilha comparados na PSRP; ponto/capacidade Eclipse próprios. Sem conversão automática de bolsas de jobs, móveis ou storage compartilhado; validar menus/acesso/regressões no GTA. Fases 8/9 mantidas.

Continuação 0.34.0: fase 6.3A com sete barbearias, corte/textura/olhos freemode, prévia reversível, cobrança explícita e último visual por perfil/modelo. Pontos e métodos PSRP comparados; getters revisados para não usar RGB de paleta como cor atual do ped. Tintura/barba/editor facial e câmera própria ficam em subfases; validar aparência/cancelamento/convívio vanilla no GTA. Fases 8/9 e demais diferenças mantidas.

Continuação 0.35.0: fase 6.3B com tintura/mechas/barba/sobrancelhas freemode, seleção sem prévia e aplicação explícita recuperável. Taxa e pedidos atômicos, estorno só antes de iniciar, sem inventar restauração de cores não capturáveis. Grupos não escolhidos preservados; corpo/rosto/modelo/roupa não alterados. Validar aparência/overlays/pendências no GTA; criador facial, maquiagem/tatuagens e demais diferenças seguem. Fases 8/9 mantidas.


Atualização 0.36.0 / fase 6.3C: acabamento ampliado com pele/maquiagem e overlays corporais opcionais, mesmo ciclo de confirmação/retomada da 0.35. Comparação de sete Apply da referência registrada na auditoria. Refinamento visual, enquadramento, ergonomia e confirmação das paletas no GTA continuam na fase 8; herança facial/criação de ped e demais diferenças não foram declaradas concluídas. Fase 9 permanece para todos os trabalhos.


Atualização 0.37.0 / fase 6.3D: herança e 20 traços implementados no freemode existente, com consulta de origens nativas, valores negativos/positivos e espera de head blend antes dos detalhes. Aplicação somente após confirmação; comparação PSRP registrada. Fase 8 ainda refinará menus/câmera/ergonomia e verificará recomposição/paletas no GTA. Criação/troca de ped e tatuagens não foram declaradas concluídas. Fase 9 continua para todos os trabalhos.


Atualização 0.38.0 / fase 6.3E: criação de cadastro e carregamento explícito de freemode M/F, com corpo/equipamento, rosto confirmado no RPCore e retorno ao protagonista. Comparação de Gender/BuildMenu/LoadCharacterToPed PSRP registrada. Falhas protegem snapshots pendentes. Refinamento de câmera/prévia/ergonomia e confirmação visual/recursos externos no GTA continuam na fase 8; não se declarou cópia completa de estados de outros mods ou aparência PSRP. Fase 9 dos trabalhos preservada.


Atualização 0.39.0 / fase 6.4A: seguro pessoal com apólices opcionais e recuperação física de destruído no local por franquia, comparação Vehicle_Insurance PSRP registrada. Sem entrega/guincho/reposição automática; garagens/fuel/storage mantidos. Fase 8 refinará menu/banner/mecânicas veiculares e conferirá reparo/recursos com mods no GTA. Licenças e demais sistemas da fase 6 continuam; fase 9 dos trabalhos preservada.


Atualização 0.40.0 / fase 6.4B: DMV C com questões próprias, prático/36 pontos e avaliação/emissão persistentes. Fonte TheoryTests tem botão vazio e progressão incompleta; diferenças documentadas na auditoria. Refinamento de leitura/menus, áudio/câmera e reconhecimento de trânsito continua na fase 8; nenhum job/veículo anterior recebeu novo bloqueio por licença. Classes A/M1 e fiscalização permanecem posteriores. Fase 9 dos trabalhos mantida.


Atualização 0.41.0 / fase 6.4C: M1 com livro/questões próprios, PCJ/examinadora na garupa, capacete e limites por trecho. Referência só anuncia M1 futura; adaptação do circuito C e escolha de garupa são implementação própria. C preservada. Refinamento de física/rota/UI/capacete e avaliação de trânsito continua na fase 8; classe A e fiscalização não declaradas concluídas. Fase 9 dos trabalhos mantida.


Atualização 0.42.0 / fase 6.4D: A com teoria própria, Phantom/carreta, engate manual, rota industrial, ré/alinhamento/desengate e documento por perfil. Referência anuncia A como futura; não se declara exame A pronto na PSRP. Rota, manobras, tráfego e ergonomia precisam de GTA; interface/mecânicas continuam na fase 8, refinamento de TODOS os trabalhos na fase 9. C/M1 e empregos antigos sem novos bloqueios. Fiscalização e demais diferenças seguem.


Atualização 0.43.0 / fase 6.5A: presença opcional de gangues nas HQs, líder/membros e editor mínimo de postos, após comparar GangSpawner/GangCreators. Sem matriz global hostil, guerras/economia ou mudança de filiação/reputação. Chão/modelos/retorno nativo precisam de GTA. Ergonomia e apresentação continuam na fase 8; refinamento de todos os trabalhos na fase 9. Territórios, recrutamento e polícia permanecem posteriores.


Atualização 0.44.0 / fase 6.5B: equipe de até 3 membros da gangue, ordens e passageiros, após comparar recrutamento/dispensa da PSRP. Preserva NPC/posto e usa tarefas locais; não toma grupo nativo nem concede armamento. Não declara escolta armada, iniciação ou territórios completos. Formação, ergonomia e navegação/assentos devem ser conferidos no GTA e refinados na fase 8; refinamento de todos os trabalhos permanece na fase 9.


Atualização 0.45.0 / fase 6.5C: iniciação por lealdade opcional no líder, dois pacotes em contatos/posições da PSRP, interação cronometrada e filiação persistida ao voltar. Guarda de atividade/recibos/retomada próprios; sem cutscene forçada ou substituição retroativa de Join. Força/direção/diálogo completo e territórios continuam pendentes. Piso/animação/input precisam de GTA; apresentação e ergonomia continuam na fase 8, refinamento de todos os trabalhos na fase 9.


Atualização 0.46.0 / fase 6.5D: iniciação por força com três desafiantes, após comparar estados/vida/área/derrotas PSRP. Sem forçar arma/cutscene, com aprovação e filiação separadas e retomada física própria. Flags de dano/rendição e luta exigem GTA; apresentação/ergonomia continuam na fase 8. Refinamento de todos os trabalhos permanece na fase 9; direção/territórios/guerras/economia/polícia continuam pendentes.


Atualização 0.47.0 / fase 6.5E: iniciação de direção após comparar passageiro/alvo/autoria/fuga/retorno PSRP. Carro do jogador somente referenciado, grupos próprios temporários e aprovação/retomada persistentes, sem alterar polícia/relações nativas. Tiros/visada/portas/autoria precisam de GTA e refinamento de interface/mecânicas na fase 8; refinamento de todos os trabalhos permanece na fase 9. Territórios/guerras/economia/polícia e diálogo completo continuam pendentes.


Atualização 0.48.0 / fase 6.5F: base de captura com 15 áreas/postos/donos PSRP, mapa opcional e posse por perfil após combate/confirmar. Não declara reforços/guerras/economia/renda ou polícia completos. Combate/piso/acesso nativo precisam de GTA. Ergonomia/leitura de área e apresentação continuam na fase 8; refinamento de todos os trabalhos na fase 9.


Atualização 0.49.0 / fase 6.5G: defesa manual em ondas e resultado de posse, após comparar atacantes/vitória/derrota PSRP. Mantém capturas antigas; falhas técnicas suspendem sem penalidade. Não declara IA de guerras/veículos/guarnição/economia/salários concluídos. Ondas, piso, combate e perda precisam de GTA; ergonomia e mecânicas permanecem para fase 8, refinamento de todos os trabalhos para fase 9.


Atualização 0.50.0 / fase 6.5H: progressão250/750/1500 e recompensas territoriais PSRP, tesouraria financiada por banco e participação solicitada com percentuais/prazo da referência. Sem criar renda/passivo ou integrar CleanCash/comércio ainda. Resultados e reputação atômicos, transferência só em Wallet com rollback. Calendário/HQ/banco precisam de GTA; apresentação/mecânicas continuam na fase 8 e refinamento de todos os trabalhos na fase 9.

## Base 0.51.0 — comércio territorial (6.5I)

Gestão legal própria, sem equivalência completa aos negócios PSRP: compra direta na área e atendimento presencial cronometrado. Refinar balcão/pontos, trabalhadores/clientes, animações, produtos e logística após validação; integrar comércio criminal/estoque e lavagem em entregas posteriores. Receita vai ao caixa existente, sem pagamento gratuito/offline. Fases 8/9 não antecipadas.

## Base 0.52.0 — frete territorial (6.5J)

Reposição ativa com três fornecedores/coletas da PSRP, caixas em mão pelo DeliveryCargo existente, carro/van do jogador e descarga no território. Reserva financeira persistente com retomada/estorno; sem recriar entidades de handles salvos. Refinar geometria de traseiras/estacionamento, caixas no chão/interior do veículo, trabalhador/cliente e variantes específicas de negócio nas etapas adequadas. Recompensa atual é lote comprado com desconto, não bônus2x de lavagem do café PSRP. Fases8/9 mantidas.

## Base0.53.0 — receita territorial e lavagem (6.5K)

Vendas ilegais optativas usam o Dealer existente na área sob posse; receita suja não entra diretamente no banco. Processamento próprio presencial gera85% ao caixa com taxa15% e arredondamento cumulativo. Refinar integração de trabalhadores/clientes/instalações, estoque criminal separado por território, logística e eventos/boosts após validação; não declara paridade com automação UTC/CleanCash/coleta da PSRP. Fases8/9 mantidas.

## Base0.54.0 — armazém criminal (6.5L)

Estoque criminal local com transferências reais entre Dealer pessoal e conta da área; modo de venda usa somente estoque local e receita suja existente. Refinar pontos/embalagens/logística/trabalhadores e catálogo criminal/armas/automação nas próximas entregas adequadas. Não importa inventário PSRP nem altera modos antigos. Interface/mecânicas e realismo dos trabalhos continuam fases8/9.

## Base0.55.0 — abastecimento criminal (6.5M)

Rota paga com SKU escolhido, três pontos de supply da referência e ciclo de carga/descarga próprio; entrega só no armazém criminal. Snapshot financeiro e recuperação sem cobrar novamente, separado do frete legal por enum compatível. Roubo de rivais, guardas/relacionamentos/loot e logística específica permanecem diferenças, não implementadas por esta compra com desconto. Refinar locais/traseiras/props e realismo nas etapas adequadas; fases8/9 mantidas.

## Base0.56.0 — roubo rival (6.5N)

Missão própria de combate/revista/retorno, três NPCs identificados com token e dois SKUs ao armazém apenas na confirmação final. Refinar comportamento de aproximação/aggro, armas/guardas, animações/carga visual, relações/reputação, escala e logística nas próximas etapas adequadas. Diferenças registradas frente ao DrugSupplyRun PSRP; não concede polícia amiga ou guerra automática. Fases8/9 mantidas.

## Base 0.57.0 — relações e acordos (6.5O)

Revista rival agora piora atitude uma vez com carga; menu F4 consulta e compra melhora na HQ por $1000/24h do jogo. Negociação é própria, não corresponde à venda em lote PSRP com30% de melhoria. Na fase8 revisar feedback/clareza do cooldown e estado Guerra; na9 integrar/refinar contextos físicos de trabalhos sem alterar contabilidade. Bulk deals, IA global/guerras, convoys/armas/boost/diálogo/polícia e recompensa MissionWin ainda não concluídos. Preserva fases8/9 como etapas futuras.

## Base 0.58.0 — venda em lote (6.5P)

Entrega física de um SKU em caixa no veículo atual e um comprador próprio; E final grava estoque/receita suja/relação30% atomicamente. Fonte PSRP tem mix, van própria, crew/carro, sequência longa, reserva inicial, integridade/dano/emboscada e MissionWin: continuam diferenças explícitas, com semântica recuperável desta base. Na8 refinar informações de lote/receita/ready; na9 refinar transporte/encontro de todos os trabalhos, sem enfraquecer snapshots/rollback/contabilidade. Fases8/9 ainda futuras.

## Base 0.59.0 — arsenal comercial (6.5Q)

Compra paga própria abastece estoque comercial separado; entrega Kind1 compartilha caixa/carro/um comprador e grava Bulk+Arms+Illicit, sem bônus diplomático/equipar armas nativas. Valores e vagas inspirados em GetWeaponSalePrice/MaxAmount; PSRP rouba van e usa crew/dano/emboscada/automação/MissionWin, ainda pendentes. Na8 refinar clareza de estoque comercial vs armas pessoais; na9 refinar logística/diálogo/animação e todos os trabalhos preservando snapshot/rollback. Fases8/9 permanecem futuras.

## Base 0.60.0 — roubo de van de armas (6.5R)

Van própria parada com3 guardas de pistola e abates com prova; volante/retorno seguro15m/E final creditam Recovered. Fonte tem comboio motorista+3 passageiros/armas de gangue/fuga por mira-tiro/caixa de madeira/escala por territórios/MissionWin: ainda diferenças para refinamento. Na8 melhorar feedback das etapas/provas e na9 estudar/refinar condução/combate/logística de todos os trabalhos, mantendo snapshots, rollback e limpeza sem apagar veículos ocupados. Fases8/9 permanecem futuras.

## Base 0.61.0 — venda automática de armas (6.5S)

Opt-in por categoria no arsenal, uma arma/$500 por180s ativos por área, semAmmo/offline/catch-up. Fonte usaUTC e até40 vendas; nossa rotina segura/livre preserva processamento manual85%. Na8 refinar exposição de tempo/pausa/preços por modalidade e histórico; na9 refinar convivência com trabalhos/fluxos físicos sem retirar proteções de estoque/rollback. Lavagem automática/boost do café e demais diferenças ainda pendentes; fases8/9 futuras.

## Base 0.62.0 — processamento automático (6.5T)

Opt-in, até500 por60s ativos e85% ao caixa; mesma contabilidade cumulativa do manual, transação em3livros e sem offline. Fonte usaUTC/até7200s/boost temporário/CleanCash. Na8 refinar clareza do tempo/condições/taxa e saldos da área; na9 refinar convivência de receitas/trabalhos sem perder exclusividade/rollback. Missão do café/boost2X e demais diferenças ainda pendentes, fases8/9 futuras.


## Base0.63.0 — café sem antecipar fases8/9

Área4 possui abastecimento grátis e bônus2X conquistado por três caixas, com pontosPSRP. Base usa carro/van do jogador e caixa na mão/checkpoints, não Burrito fornecida/carga visível na van. Fase9 deve avaliar veículo, volumes físicos/animacões, tempos/distâncias/dificuldade e MissionWin com consistência entre arquivos; fase8 refinar apresentação/contador atualizado e necessidades sem remover compatibilidade. ValidadeUTC/histórico/snapshot/replay/rollback e taxa85% são invariantes a preservar. Gameplay ainda precisa de testes do usuário. Demais atividades de bônus, diálogo/guerras/polícia continuam no roteiro.


## Base0.64.0 — panfletagem sem antecipar fases8/9

Divulgação da tatuagem usa pedestres civis reais e animação do jogador, faixa/contagem/loja da PSRP. Não congela/toma controle de NPC de rua e não cria clientes artificiais; densidade local/alcance precisam de teste GTA. Fase8 deve refinar painel/ícones/tempo visível; fase9 avaliar prop de papel, gesto/diálogo, ritmo de clientes e balanceamento, preservando exclusão de NPCs de missão/grupos/gangues, cinco clientes distintos, recompensa apenas na volta, taxa85%, atomicidade/snapshot/recibos e bônusUTC não cumulativo. Mecânico/guincho e demais atividades de território seguem no roteiro.


## Base0.65.0 — oficina territorial

Base reproduz três clientes,120 modelos, chegada/dano/capô/animação/minigame e2X da PSRP, sem teleporte do carro para vaga. Fase8 refinar painel/barra/legibilidade; fase9 estudar física e dimensões da vaga por modelo, motores dianteiros/traseiros, chegada/saída/navmesh, fumaça/animação/prop de ferramenta, dificuldade/faixa verde e ritmo. Preservar carros concluídos/snapshot/recibos/rollback, carro pessoal e cleanup sem ejetar, taxa85% e bônusUTC não cumulativo. IA/tráfego precisam de GTA; guincho/demais trabalhos seguem no roteiro.


## Base0.66.0 — guincho territorial

Engate físico nativo com guincho fornecido, duas recuperações e nota de danoPSRP. Fase8 refinar HUD/braço e atalhos configuráveis; fase9 estudar alinhamento por modelo, física de rodas/gancho, capacidade/danos/dificuldade, interação com cliente e trajetos/ocupação dos15 pontos. Preservar H, scripts nativos de guincho, carros pessoais, prova de descarga comandada, exclusão de ocupados/ligações externas, snapshots/recibos/rollback, recuperação salva e bônusUTC não cumulativo/85%. Coexistência controller_towing e física precisam de GTA; base não declara refinamento concluído.


## Base0.67.0 — transporte de valores

Stockade/cinco seguranças/moneybags carregados fisicamente com offsetsPSRP e entrega ao recebedor. Fase8 refinar HUD/ícones/carga/bônus; fase9 avaliar segurança simultânea/diálogos/ritmo/gestos/props por ped e modelo, portas do blindado, acompanhante/rota/ameaças/danos/dificuldade e embarque/saída. Preservar preferênciasUseProps, carro pessoal, cleanup de ocupados, provaDelivered/replay/rollback, parcialrefaz e pronto persistido,85% e bônusUTC não cumulativo. Gameplays/native armored-truck coexistence ainda precisam do GTA; não declara refinamentos concluídos.


## Base0.68.0 — documentos médicos

Base segue médico/papel/locaisPSRP com coleta/entrega a pé e trânsito livre em veículo, persistênciaCollect/Delivered. Fase8 refinar HUD/painel/ícones/estado de documento; fase9 avaliar diálogos, recepção no destino, gestos/offset por ped, ocultação na entrada/saída e risco/tempo/dificuldade. Preservar coleta salva, carros pessoais, preferências de props, provaDelivered/replay/rollback,85% e bônusUTC não cumulativo. Hospital/MLO/gestos/bones e carro precisam de GTA; base não declara fases8/9 concluídas.


0.69.0 / fase6.5AA: loja temporária Legion do território7 implementada conforme catálogo/props/clientes/checkout da PSRP. Refinar HUD, caminho dos clientes, gestos e realismo nas fases8/9, depois de validação no GTA. Receita suja por venda confirmada persiste ao cancelar; não converter em pagamento pessoal. Guia FASE-6.5AA-0.69.0.md.


0.70.0 / fase6.5AB: jardinagem do território8 implementada conforme57 pontos/veículo/grama da PSRP. Refinar HUD e trabalho na fase8/9 após testar raio, props, acesso, caminho e limpeza no GTA. Sem pagamento pessoal ou renda criada; cortes persistem e meta concede bônus2X. Guia FASE-6.5AB-0.70.0.md.


0.71.0 / fase6.5AC: estreia no território9 implementada conforme11 itens/caixa/instalações/IPL da PSRP. Refinar HUD/carregamento/instalações e realismo nas fases8/9 após teste no GTA. Confirmar modelo da última peça(label Red Carpet usa cerca na fonte) e visual do IPL, anim/bone/solo e convivência com outros recursos. Guia FASE-6.5AC-0.71.0.md.


0.72.0 / fase6.5AD: motorista VIP no território10 implementado com modelos/destinos/perfis/prazo/nota da PSRP e bônus graduado. Refinar HUD, rota/acesso, porta e embarque IA, conforto/prazo e retomada parcial nas fases8/9 após teste no GTA. Guia FASE-6.5AD-0.72.0.md.


0.73.0 / fase6.5AE: frete do porto no território11 implementado conforme rig/carga/descarga/fórmula da PSRP. Refinar HUD, acesso/baia/rota, offsets de paletes e condição nas fases8/9 após teste no GTA. Fórmula da referência reduz dano a1/3, mínimo66,67% com base1000 e corpo não negativo; não confundir com carroceria bruta. Guia FASE-6.5AE-0.73.0.md.


0.74.0 / fase6.5AF: circuito BMX do território12 implementado conforme11 pontos/modelo/raio/altura/prazo da PSRP. Refinar HUD, navegação em rampas, quedas/retomada/prazo nas fases8/9 após teste no GTA. Não inventar exigência de manobras ausente na validação da fonte. Guia FASE-6.5AF-0.74.0.md.


0.75.0 / fase6.5AG: entrega offshore do território13 implementada conforme PSRP. Refinar HUD/cinematografia compatível, água/ondas, acesso ao Seashark e comprador, transferência e retomada nas fases8/9 após teste no GTA. Guia FASE-6.5AG-0.75.0.md.


0.76.0 / fase6.5AH: manutenção do píer no território14 implementada com21 pontos reais da PSRP(2 reposições+19 regas); toast da referência diz7 incorretamente. Refinar HUD, acesso/rumos/animação/regador e realismo nas fases8/9 após teste no GTA. Guia FASE-6.5AH-0.76.0.md.


0.77.0 / fase6.5AI: caixa24/7 do território15 implementado com52 produtos/40 modelos/scans e salário400 da PSRP; mensagem da fonte anuncia bônus sem código para ativá-lo, não inventar recompensa. Refinar painel/cesta, rota/atendente/uso externo do balcão e realismo nas fases8/9 após teste no GTA. Guia FASE-6.5AI-0.77.0.md.


Atualização0.78.0 /6.5AJ: pacote de declaração diplomática, alertas120s/defesa integrada, simulação rival distante e reforços a pé opcionais. ComparaçãoPSRP documentada, sem globalrelationships/produçãooffline. Fase8 revisará leitura/menus/mapa e relógios associados; fase9 aprofundará combate/chegada de reforços/física/dificuldade e variantes. Comboios motorizados/assaltos organizados/drive-bys/diálogos completos ainda não entregues. Pendências agrupadas em PENDENCIAS-ANTES-REFINAMENTO.md; nenhuma conclusão antecipada de6/7/8/9.


Atualização0.79.0 /6.5AK: pacote integrado de preparação de quatro carros, escoltas, chegada salva com prova de motorista/uma escolta, desembarque voluntário, equipe de até15aliados e captura existente. Comparação dos60pontos/rumos e métodos de comboio PSRP registrada. Fase8 revisará seleção/objetivos/ícones/leitura de relógios; fase9 aprofundará condução/recuperação de IA, saúde/munição/dano/posição dos sobreviventes, tuning e variantes físicas. END/reload conservam baixas/chegada/clocks, sem salvar handles. Drive-bys/diálogos completos e variantes de reforço/roubo motorizado não declarados entregues; polícia/fase7 pendentes. Nenhuma conclusão antecipada de8/9.


Atualização0.80.0 /6.5AL: pacote de emboscadas optativas(aproximação/tiro/fuga/encerramento) e conversa presencial com líder ligado às iniciações/gestão existentes. Métodos/constantesPSRP e tipos0/1/2 comparados. Sem arma/vida/imunidade do jogador ou matriz global hostil; reload interrompe eventos, sem replay/prêmio. Fase8 revisará desenho do diálogo/menus/ícones/ergonomia; fase9 aprofundará direção/tiros/LOS/dificuldade/tuning/variantes. Fallback de co-líder, variantes móveis de roubo/reforço motorizado e demais diferenças na revisão de paridade, sem declarar equivalência total. Polícia/prisão e fase7 seguem na fila; não antecipadas8/9.


Atualização0.81.0 /6.6A: pacote de fiscalização7infrações/162limites, observação contínua/LOS, patrulha/resposta própria/abordagem e dívida/pagamento bancário atômico. PSRPBetterPolice comparada; PrisonSystem só suprime polícia globalmente e não é portada. Fase8 revisará apresentação/lista/limite/contexto/cinto/HUD; fase9 aprofundará navegação/estacionamento/gestos/dificuldade e integração física. Perseguição/custódia/prisão e infrações restantes exigem etapas funcionais posteriores, não são declaradas refinamento já entregue. Trabalho prisional antigo separado; fases8/9 preservadas.

Atualização0.82.0 /6.6B: ciclo próprio de rendição/custódia/pena/fiança/liberação integrado à perseguição nativa. Interior/piso verificados, fichamento antes de limpar uma única perseguição voluntariamente entregue; retomada explícita sem apagar novas estrelas. Sem supressão global da referênciaPrisonSystem. Fase8 revisará apresentação/ergonomia/contadores; fase9 aprofundará transporte físico, procedimentos/animações e realismo. Perseguição independente/escalada, infrações restantes e integração opcionalLSPDFR são diferenças funcionais ainda abertas; fase7 segue. Não são declaradas entregues pelo nome de método ou existência de menu.

Atualização0.83.0 /6.6C: quatro checks restantes deEvaluateViolations e perseguição própria carro/pé com reforço nativo opcional. Fase8 revisará apresentação/ergonomia; fase9 aprofundará condução/estacionamento/gestos, dificuldade e filtros. Suborno/BOLO automático/painelLSPDFR não declarados implementados; revisão final distinguirá função ausente e diferença deliberada. Próximo bloco fase7, sem abrir uma fase por variante policial.

Atualização0.84.0 /7.1: pacote integral dos seis atos deThe Hollywood Act, comparado à referência2463linhas. Guardas/veículos/props próprios, etapas físicas, preparações salvas e prêmio bancário com prova/recibo. Fase8 revisará apresentação/F2Lester/F11/fechadura e HUD; fase9 aprofundará missões, furtividade, diretor/navmesh, portaria, combate/reboque/mar/câmeras/animações. Diferenças deliberadas de polícia/ambient takeover/imunidade/cleanup e distração sem reduzir estrelas estão no guia/auditoria; não se declara paridade visual/física total. Halloween/editor/revisão final ainda na fase7.

Atualização0.85.0 /7.2: Halloween funcional comRomero/corrida180s, perseguição e pulso8s/12m em recursos próprios, sustosYTD/áudio existentes e histórico/recarga sem recompensa. Fase8 revisará painel/opções/efeitos; fase9 aprofundará rota/dificuldade/ondas/ram/física/controle. Trânsito ambiente/clima/relogio/câmera globais preservados como diferença deliberada. Teste GTA pendente; editor e revisão final ainda na fase7, sem antecipar8/9.

Atualização0.86.0 /7.3: editor próprio de props/ícones/locais com biblioteca por perfil, rascunhos, persistência/import/export e serviços existentes. Fase8 revisará menus/controle/apresentação; fase9 aprofundará colocação, ergonomia e integrações físicas. ReferênciaMapBuilder não contém editor de props e faz limpeza global de blips_C, não portada. Pontos de oficina entregam reparo/lavagem existentes; tuning completo daPSRP permanece lacuna funcional para revisão final, não é declarado refinamento já implementado. Teste GTA pendente; revisão final é o próximo bloco.

Base0.87: oficina personalizada funcional; fase8 também tratará apresentação/paletas/RGB e opções adicionais, sem afirmar equivalência total da PSRP. Táxi/limousine de passageiro e controles de clima/tempo ainda são sistemas ausentes para implementação anterior ao refinamento. Consulte REVISAO-PARIDADE-0.87.0.md.

Base0.88: transporte de passageiro e controles de mundo agora têm ciclo funcional próprio, guia e testes de dados; a nota0.87 de ausência é histórica. IA/taxímetro físico, clima/relógio e HTTP no GTA não foram homologados. Variantes de apresentação/realismo ficam8/9 depois da classificação final de gangues/polícia.

Transição0.89: base independente encerrada conforme FECHAMENTO-BASE-0.89.0.md. Próxima entrega inicia8: mecânicas/HUD/menus, incluindo fome/sede.9 inclui todas as atividades/gangues/missões e variantes ausentes de alvos móveis/reforços motorizados/tuning. LSPDFR opcional não implementado/fora da base; sem afirmar paridade total.

Pedido atual: RP ativo por padrão, menos comandos/toggles no fluxo normal, menus organizados/fidelidade PSRP e investigação de bugs.0.90 inicia8 com preset uma vez por perfil, necessidades/consumo durável, stamina/oxigênio/avisosFitness, F11 oito categorias/F10 quatro grupos e grade6x4. Novos falsos antigos de perfil são habilitados conforme pedido; ajustes avançados posteriores preservados. Não conclui todos os menus/bugs ou a fase8 inteira.

Correção solicitada após0.90: usuário confirmou queda repetida apesar de comer/dormir na casa Franklin.0.91 troca queda30s sem restauração por episódio10s com sono/fôlego cheios, protege veículo/embarque e correlaciona cama nativa com cena/relógio. Teste GTA pendente; não é aceite da fase8 inteira.


Diretriz permanente confirmada pelo usuario: todo refinamento deve partir da leitura dos handlers e fluxos relevantes da PSRP.dll. Registrar comportamento da referencia, implementacao RPCore, divergencias deliberadas e roteiro GTA; corrigir bugs da referencia, sem declarar paridade por nomes de classes.

0.92: armas/municao no inventario, equipar/guardar, roda de armas opcional, pilhas de arremessaveis e transferencias com metadados. Baseada em NewInventorySystem/SettingsMenu/ItemDatabase, sem Give250 ou desconto por simples botao. Usa tipos reais de municao GTA. Ferramentas utilitarias continuam nativas; lojas nativas observadas, sem nova loja paralela. Fase8 continua; fase9 preservada.


0.93, revisão agrupada do inventário solicitada pelo usuário: atalhos1..6, filtro de input/modais, rolagem/peso/numeração, duas grades de armazenamento e correção24slots/posições altas,63 nomes/ícones,31 itens novos e9 receitas pessoais, uso6.5s/props próprios, energia incremental idempotente/retornos no consumo, compras salvas. Varredura315métodos/5arquivos da referência. Guia INVENTARIO-REVISAO-0.93.0.md lista explicitamente ferramentas/intoxicação ainda distintas e as integrações de estoque/caça/pesca/drogas/roubos da9; nenhum estoque/pagamento de trabalho foi convertido automaticamente. Não declara toda a fase8 ou a paridade PSRP concluídas.


0.94: mouse na grade pessoal solicitado após aceite do inventário0.93. Extensão própria sobre a geometria PSRP: selecionar/arrastar/trocar posições, roda, cancelamento, mesmo salvamento existente. Sem mudar quantidades, configurações ou fluxo de transferência/receitas.3602 testes de dados aprovados; cursor e gestos GTA pendentes. As demais diferenças da revisão0.93 continuam listadas no guia, sem abrir nova revisão de atividades.

Antes da publicação0.94, usuário pediu peso funcional: sobrecarga automática com redução progressiva de movimento, sprint/salto bloqueados a pé, exclusões de veículo/missão/controle/água/ar e efeito por frame sem multiplicadores globais. Quantidade/pesos/limites existentes conservados. Ctrl+Enter no depósito transfere o que couber para facilitar aliviar estoques legados. Incluído no mesmo pacote de mouse; teste físico GTA pendente.


### Correção 0.95.0 após teste da 0.94.0

Usuário confirmou mouse, sobrepeso, normalização e atualização por munição/exclusão. Relatou props presos após consumo e roda reativada; corrigidos os caminhos de limpeza confirmada/repetida e política de input independente da armaria. Delete agora larga itens recuperáveis no chão, persistidos com inventário, R/Shift+R recolhem perto da marca. Fonte PSRP revisada em NewInventorySystem/SettingsMenu; descarte recuperável é código próprio. Teste físico pendente conforme CORRECOES-INVENTARIO-0.95.0.md. Demais ferramentas/intoxicação e refinamento dos trabalhos continuam no escopo registrado.


### Refinamento 0.96.0 — lojas e integração visual de consumo

NewShopSystem/ItemDatabase revisados. Conveniências e Grocery do editor ganham catálogo por categorias/grade6x4, mochila somente para consulta, quantidades e compra explícita limitada por dinheiro/peso, input modal com gate e saída segura. Preços/forma de pagamento/configs/pontos anteriores preservados. Encontrada segunda criação de prop por PropPresentation.Used: removida assinatura interna duplicada e centralizados bindings/fallbacks na apresentação identificada do inventário. Celular conserva seu apresentador.3686 testes aprovados; GTA ainda precisa roteiro LOJAS-E-CONSUMIVEIS-0.96.0.md. Ferramentas, intoxicação, lojas especializadas e fase9 permanecem no escopo aberto.


### Confirmação do escopo integral e refinamento0.97

Usuário confirmou teste da0.96 funcionando e reforçou que todo sistema implementado, todos os menus e detalhes devem ser refinados. COBERTURA-REFINAMENTO.md registra os58 sistemas ISystem e os componentes auxiliares; base funcional não equivale a revisão completa.0.97 revisa SettingsMenu/RadialMenu da PSRP: checkboxes/listas e preferências gravadas antes de aplicar, escrita temporária+backup, páginas de uso de inventário no radial, mouse/voltar/input seguro, GPS de serviços reais, marcadores com renomear/confirmação/gravação candidata e gestos curtos de mãos para cima. Teleporte/F2/J/F11 preservados.3725 testes; GTA0.97 pendente. Outras páginas, menus e fase9 continuam explicitamente abertos.


### Refinamento0.98 — celular F2 e F10

CustomPhone/MoneyTerminal/SettingsMenu consultados: listas/detalhes/leitura individual, notas, cronometro/temporizador, banco com valor escolhido. Celular ganha mouse/rolagem, historico64 acessivel com leitura individual, notas transacionais/detalhes/exclusao confirmada, temporizador e pausa/continuidade do cronometro sem credito offline. Servicos diretos corrigem conflito GarageKey/F11. F10 conserva4grupos e organiza importacoes/perfis com confirmacao, banco custom/max/100 via BankTransfer existente, erro isolado e refresh dos submenus. Phone prop recebe decor/propriedade/remoção repetida, sem repetir criacao de consumiveis.3772 testes; GTA0.98 pendente. Nao declara voz/SMS externos, todos os apps PSRP, emprestimos bancarios ou fase8 inteira concluidos.


### Refinamento0.99 — veiculos/garagens/oficina

DrivingFeatures/RadialMenu e controles/pecas da referencia SettingsMenu consultados. Radial veicular por paginas e menu K compartilham acoes; corpo requer parado/modelo/porta, vidros com comando explicito sem tratar vidro intacto como posicao; alvos amarrados a ator/perfil/carro. Neons observados ganham prova opcional por identidade em Transport.NeonEquipment antes de apagar, sem conceder instalacao gratuita. F11/GPS acessam veiculo/garagem/concessionaria diretamente. Oficina agrupada com navegacao/atualizacao diferidas, selecao preservada, protecao de repeticao e RGB personalizado por compra existente. Garagem confirma remocao pelo handle original/identidade, resolve registro atual e revalida contexto/saida apos streaming; reparo/lavagem atualizam snapshot imediatamente com rollback comum.3811 testes; GTA0.99 pendente. Custos/chaves/carros/saves/missoes/trabalhos preservados; nao conclui toda fase8 nem fisica da9.


###0.100.0 — serviços pessoais

Comparados os fluxos de roupa/barbearia de MultiplayerCharacter, páginas de roupas/GPS do radial e acesso do Eclipse em Apartments.14 lojas com E/ícones/GPS e biblioteca anterior; menus dos quatro serviços com despacho após LemonUI, provas de contexto/página, seleção, retorno e confirmação sem repetição. Acabamento dividido em categorias com resumo de aplicação; confirmações para substituir/excluir roupa e comprar moradia. Cancelamento conserva alterações externas, e snapshots de personagens não capturam prévias durante o lease. Preços/dados existentes e inventário privado do apartamento preservados.3845 testes aprovados, Release sem erros/avisos. Guia SERVICOS-PESSOAIS-0.100.0.md. Câmera dedicada e validação GTA não declaradas prontas; criação/carregamento completo de personagens, DMV/seguro, demais grupos da fase8 e todos os ciclos da9 seguem na cobertura.


###0.100.1 — portas de roupas

Correção do bloqueio físico relatado pelo usuário na0.100.0. Comparados ShopsDoorsLocation/UnlockDoors/Update:14 entradas externas e quatro modelos de porta de roupas, com consulta nativa de trava/ângulo, proximidade/altura/contexto e associação às lojas configuradas. Não copia o desbloqueio mais amplo de Ammu-Nation, garagens, barbearias ou concessionária.3855 testes aprovados; roteiro GTA em PORTAS-ROUPAS-0.100.1.md. Não marca nova frente de refinamento como concluída.


###0.101.0 — RPCore Bank solicitado pelo usuário

Entrega adicional ao roteiro: app bancário no próprio celular com tema roxo, depósito/saque digitados e todo o disponível, conferência e comprovante. Conta compartilhada com o F10 pelo Wallet/BankTransfer existente; sem taxas/cap diário/valores fixos, mantendo o limite numérico anterior para compatibilidade. Cotação vinculada ao perfil/ator/modelo/saldos e consumida uma só vez; rollback comum e input com liberação.3889 testes aprovados; build sem erros/avisos. Guia RPCORE-BANK-0.101.0.md. Não encerra outras frentes da fase8 ou os ciclos da9.


###0.102.0 — entrega agrupada: bolsa, rádio e conquistas

Usuário aprovou RPCore Bank0.101 e reforçou não publicar refinamentos isolados. Foram revisadas juntas todas as telas/ações de StockMarketSystem, RadioSystem e AchievementSystem, comparando o módulo StockMarketMod e os fluxos RadioPlayerIRL/AchievementManager. Bolsa inclui busca/ordenação/posições/favoritos/funding/ordens, validação/rollback/legado; rádio inclui controles/lista/XML/lifecycle/estado/prefs; conquistas incluem filtros/detalhes/progresso/persistência/eventos/unknowns.3915 testes aprovados, build sem erros/avisos. Guia BOLSA-RADIO-CONQUISTAS-0.102.0.md. Grupo de código revisado; validação nativa pendente. Mercado local/COM anterior preservados; não declara fase8 inteira encerrada nem altera ciclos da9.


###0.102.1 — hotfix de duplicação relatada no banco

Grupo0.102 aceito pelo usuário; identificado bug de depósito seguido de carteira antiga restaurada pelo save do GTA. Carteira nativa passa a checkpoint Owner/Amount/Version no mesmo Wallet do banco, com sincronização única antes das ações após loading e stats específicos por protagonista, em vez de escolher pelo ator atual. Saves sem marcador preservam carteira atual; MP permanece no Wallet, sem confisco retroativo de valores já duplicados.3933 testes, incluindo18 regressões. Guia BANCO-PERSISTENCIA-0.102.1.md exige fechamento completo do GTA. É correção pontual de bug relatado, não novo passo fragmentado de refinamento.


###0.102.2 — estabilização antes de retomar refinamento

Usuário enviou logs que confirmam Bool boxed no OutputArgument das portas e menu vazio no F10; causa do fechamento nativo durante jogo/perseguição não aparece. Corrige essas falhas, handles boxed de cleanup, polling/bind veicular desnecessário e input de menu oculto. Adiciona diagnóstico mapeado de8KB com resumo na sessão seguinte, sem concluir culpado por contexto.3940 testes aprovados, build sem erros/avisos. STABILIDADE-0.102.2.md orienta teste de portas/F10/direção/banco e coleta se cair. Hotfix inclui0.102.1, ainda não testada pelo usuário. Avanço de refinamento pausado para validação de estabilidade.


###0.103.0 — entrada com personagens

Usuário confirmou estabilidade inicial/portas/F10 e pediu lista ao entrar, criação se vazio e opção para desativar. Implementados no grupo de menus de personagens com ações diferidas e confirmação, conservando troca e saves existentes. Consulte PERSONAGENS-INICIO-0.103.0.md. Não conclui editor completo nem fase9.


###0.104.0 — grupo completo de armazenamento existente

Retomada autorizada pelo usuário. Referência NewStorageSystem/NewInventorySystem/Vehicle_Insurance conferida. Porta-malas, depósitos e apartamento revisados juntos nas apresentações grade/lista, quantidades e gesto entre painéis, contexto, armaria e checkpoint conservador.3988 testes; roteiro ARMAZENAMENTO-0.104.0.md. Animação automática da tampa não portada. Não declara fim da fase8 nem realismo de trabalhos da9.


###0.104.1 — correção prioritária da entrada/troca

Relato de crash na0.103 interrompe avanço normal. Logs confirmam retorno ao perfil Franklin logo após entrada; causa fatal não consta e RuntimeLastSession é da0.102.2 anterior. Proteções da entrada/manual, espera no novo ped e diagnóstico por fase implementados.4011 testes; aguarda teste GTA conforme PERSONAGEM-ESTABILIDADE-0.104.1.md. Não conclui causalidade nativa nem altera o escopo da fase9.


###0.104.2 — substituição do caminho que travou

Após novo crash, RuntimeState0.104.1/6020 e log sem retorno do SET_PLAYER_MODEL feminino localizam a fronteira nativa. Fluxo próprio com ped preparado/CHANGE_PLAYER_PED, confirmação nativa e descarte apenas de candidato não controlado, preservando saves.4029 testes; PERSONAGEM-HANDOFF-0.104.2.md. Aguardar teste real; não atribuir a falha a roupa/saldo ou afirmar solução definitiva antes dele.


###0.104.3 — bloqueio de decor confirmado

Log0.104.2 identifica falha ao exigir decor antes do CREATE_PED. Caminho substitui a exigência por registro de propriedade da sessão e endereço do ped; conserva limpeza restrita, saves e ausência do SET_PLAYER_MODEL.4047 testes. PERSONAGEM-REGISTRO-0.104.3.md orienta validação GTA. Transferência/crash anterior ainda precisam de confirmação; avanço de refinamento normal permanece pausado.


###0.104.4 — restauração limitada aos personagens

Usuário pediu voltar só personagens à0.102.2 por duplicação de peds, mantendo o restante. CharacterSystem e Main.SwitchCharacter exatamente restaurados; entrada automática/espera/handoff/helpers/testes exclusivos removidos. Armazenamento0.104 e demais sistemas atuais preservados.3974 testes ativos. PERSONAGENS-RESTAURADOS-0.104.4.md orienta fechar totalmente GTA para descarregar duplicatas da sessão e testar manual F10.


###0.104.5 — persistência de roupas, mantendo troca confirmada

Troca0.104.4 aprovada; arquivos revelam aparência pendente sem Body e biblioteca com peças zeradas. Ajusta conclusão/ordem da aparência e confirmação durável/revisões da roupa; Main.SwitchCharacter continua0.102.2, sem menu inicial/handoff.3996 testes. ROUPAS-0.104.5.md explica limites do kit recebido e roteiro GTA. Não avança realismo de trabalhos.


###0.105.0 — abastecimento e cinto

Próximo grupo autorizado: revisão conjunta do menu/pedido/persistência/contexto de combustível e estado/saída/restauração do cinto, baseada em Vehicle_Insurance/DrivingFeatures/UiSystem PSRP.4038 testes, build sem erros/avisos. Personagens/roupas0.104.5 e contratos mantidos; fase9 não iniciada. ABASTECIMENTO-CINTO-0.105.0.md contém roteiro nativo.


###0.106.0 — concessionária e seguro

Revisão conjunta de catálogo, seleção de garagem, confirmação, criação/compra/rollback, navegação/apólices/franquia/retomada/estorno. Referência estática Dealerships e Vehicle_Insurance PSRP; não implementa previews/test-drive/catálogo amplo/entrega da seguradora.4079 testes e build net48 limpo. Guia CONCESSIONARIA-SEGURO-0.106.0.md. Personagens/roupas, combustível/cinto e ciclos dos trabalhos preservados; fase9 não iniciada.


###0.106.1 — correção relatada: portas e menus PDM

Portas/showroom, aviso de acesso e fila dos12menus PDM, com isolamento de falhas de banner/fechamento. Não muda ciclos de trabalhos nem as transações/saves da0.106.0.4105 testes e build limpo; falha de HUD relatada não reproduzida no cloud, teste GTA conforme PORTAS-PDM-MENUS-0.106.1.md e logs se persistir.
