## Hotfix0.104.2 — alternativa à substituição do ped

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.104.2.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.104.2.zip) · [Teste GTA e limites](docs/PERSONAGEM-HANDOFF-0.104.2.md)

Logs e RuntimeState da0.104.1 param em SET_PLAYER_MODEL do freemode feminino, sem retorno; a espera não resolveu. O carregamento passa a criar um ped identificado e transferir controle com CHANGE_PLAYER_PED, sem chamar novamente aquele native. Conserva personagens/saldos/pendências e só descarta candidato próprio não controlado.4029 testes, build sem erros/avisos; o fluxo alternativo precisa do teste GTA antes de declarar o crash resolvido. Inclui0.104 e0.104.1.

## Hotfix0.104.1 — entrada e troca de personagem

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.104.1.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.104.1.zip) · [Teste e coleta GTA](docs/PERSONAGEM-ESTABILIDADE-0.104.1.md)

Após relato de crash na0.103, logs confirmam retorno ao perfil Franklin dois segundos depois de iniciar. Entrada/manual passam a aguardar30s iniciais e10s estáveis, checar troca de protagonista/colisão e revalidar a origem após salvar. Aplicação no novo ped é diferida e as fases ganham diagnóstico.4011 testes aprovados, build sem erros/avisos. Causa fatal não aparece nos logs: precisa de teste GTA, sem afirmar todos os crashes resolvidos. Inclui armazenamento0.104 e preserva a opção F11.

## Atualização0.104.0 — grupo armazenamento

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.104.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.104.0.zip) · [Acessos e roteiro GTA](docs/ARMAZENAMENTO-0.104.0.md)

Refina porta-malas, depósitos e armazenamento do apartamento juntos: grade com transferência por mouse nos dois sentidos, peso/capacidade dos dois lados, quantidade digitada e máxima; menu em lista completo com resumo/categorias/retorno; sessão ligada ao ator/perfil/veículo e atividade, sincronização de armas e saves conservados.3988 testes aprovados; build sem erros/avisos. A apresentação/input real e os natives precisam do teste GTA. Grupo0.103 e correções anteriores incluídos.

## Atualização0.103.0 — seleção de personagens na entrada

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.103.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.103.0.zip) · [Opção e testes GTA](docs/PERSONAGENS-INICIO-0.103.0.md)

Ao entrar, abre a lista dos personagens salvos quando o GTA estiver seguro; sem personagens, abre criação. Seleção manual, confirmações, perfis separados e registros protegidos. Ativado por padrão, desativável no F11 → Game Settings; acesso manual pelo F10 permanece.3954 testes aprovados; build sem erros/avisos. Carregamento real de ped/interface ainda requer teste GTA.

## Correção0.102.2 — portas/F10 e estabilidade investigada por logs

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.102.2.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.102.2.zip) · [Teste e coleta de diagnóstico](docs/STABILIDADE-0.102.2.md)

Logs confirmaram OutputArgument incompatível nas portas e seleção em menu vazio no F10. Corrige ambos e o mesmo padrão em cleanup de props; controles veiculares deixam de consultar estados de menu fechado. Acrescenta registro leve de etapa da sessão para investigar fechamentos sem stacktrace.3940 testes aprovados, build sem erros/avisos. Já inclui banco0.102.1 e os grupos anteriores. Causa de todos os crashes nativos ainda não confirmada; roteiro GTA e novos arquivos de diagnóstico no guia.

## Correção0.102.1 — carteira duplicada ou perdida ao reabrir o GTA

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.102.1.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.102.1.zip) · [Teste de fechamento completo do GTA](docs/BANCO-PERSISTENCIA-0.102.1.md)

Carteira nativa do protagonista passa a ter checkpoint com proprietário no mesmo Wallet do banco, restaurado uma vez após carregar. Corrige depósito400mil seguido de carteira duplicada e saque1,8milhão seguido de GTA voltando aos400mil antigos. Saves antigos sem marcador adotam a carteira atual, sem zerar valor legítimo; não há desconto retroativo de dinheiro já duplicado.3933 testes aprovados; build sem erros/avisos. Grupo0.102 permanece incluído; resta validar fechamento/abertura real no GTA.

## Atualização0.102.0 — grupo bolsa, rádio e conquistas

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.102.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.102.0.zip) · [Guia do grupo e testes GTA](docs/BOLSA-RADIO-CONQUISTAS-0.102.0.md)

Revisão conjunta de todas as telas/ações dos três sistemas: bolsa com busca/ordenação/carteira/favoritos, quantidade/máximo, funding e confirmação; rádio com estações/pausa/volume/prev-next e ciclo seguro do player; conquistas com filtros/detalhes/progresso salvo antes de anúncio e registros legados preservados.3915 testes aprovados; build sem erros/avisos. App RPCore Bank aprovado permanece incluído; saldos/configs/recursos anteriores preservados. Validação nativa do grupo ainda necessária.

## Atualização0.101.0 — RPCore Bank no celular

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.101.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.101.0.zip) · [Como usar e testar](docs/RPCORE-BANK-0.101.0.md)

F2 > RPCore Bank: app próprio roxo/branco, saldo em conta/carteira, depósito e saque digitados ou todo o disponível, conferência e comprovante. Mesma conta do F10, sem taxas/limite diário/valores fixos; preserva o limite numérico GTA e os saves. Proteções de contexto/saldo/uso único e gravação pelo pipeline anterior.3889 testes aprovados; build sem erros/avisos. A correção de portas0.100.1 permanece incluída; teste nativo do app ainda necessário.

## Correção0.100.1 — portas das lojas de roupas

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.100.1.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.100.1.zip) · [Instalação e testes GTA](docs/PORTAS-ROUPAS-0.100.1.md)

Corrige o acesso físico omitido na0.100.0: destrava portas de roupas ao chegar perto das14 entradas da referência, conservando ângulo. Somente quatro modelos de roupas, com alcance/contexto limitados e vínculo às lojas configuradas; sem desbloqueio global ou alteração de saves/RPF.3855 testes aprovados; build sem erros/avisos. É necessário testar entrada/saída e streaming no GTA.

## Atualização0.100.0 — roupas, barbearia, acabamento e moradia

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.100.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.100.0.zip) · [Uso e testes GTA](docs/SERVICOS-PESSOAIS-0.100.0.md)

14 lojas de roupas da referência com E/ícones/GPS e a mesma biblioteca do apartamento. Menus de serviços pessoais com ações após LemonUI, navegação, seleção e proteção contra confirmação repetida; confirmação de substituir/excluir roupas e comprar moradia. Acabamento em categorias com revisão antes de pagar. Cancelamento preserva mudanças externas e autosave de personagens não captura prévias não confirmadas.3845 testes aprovados; build sem erros/avisos. Preserve saves/configs e teste os fluxos nativos conforme o guia.

## Atualização0.99.0 — veículos, garagens e oficina

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.99.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.99.0.zip) · [Uso e testes GTA](docs/VEICULOS-GARAGENS-OFICINA-0.99.0.md)

Radial veicular por páginas e menu K compartilham motor/cinto/piloto/luzes/portas/vidros com contexto do mesmo carro; F11/GPS acessam sistemas diretamente. Oficina agrupada, retorno/seleção/refresh seguro e RGB digitado por compra existente. Neon observado pode ser religado após reload sem instalar lados novos. Garagem verifica remoção pela identidade original, usa snapshot atual e revalida saída após streaming; reparo/lavagem salvam imediatamente.3811 testes aprovados; compilação sem erros/avisos. Teste GTA ainda necessário; preços/chaves/carros/saves/configs preservados.

## Atualização0.98.0 — celular F2, banco e F10

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.98.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.98.0.zip) · [Uso e testes GTA](docs/CELULAR-BANCO-F10-0.98.0.md)

Celular com mouse/rolagem, histórico completo e leitura individual, notas com visualização/edição/exclusão confirmada e gravação candidata, cronômetro pausável e temporizador. Serviços diretos corrigem Garagens abrir F11 por conflito de teclas; telefone ganha remoção identificada/repetida do prop. F10 mantém4grupos, banco por valor escolhido/máximo/$100, saldos atuais e confirmação de importações/perfis legados.3772 testes aprovados; compilação sem erros/avisos. Saves/configurações preservados; testes GTA do grupo0.98 ainda necessários.

## Atualização0.97.0 — F11, radial e GPS refinados

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.97.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.97.0.zip) · [Uso e testes GTA](docs/MENUS-RADIAL-GPS-0.97.0.md) · [Cobertura de todo o refinamento](docs/COBERTURA-REFINAMENTO.md)

F11 com checkboxes/listas e preferências gravadas antes de aplicar, staging e backup. Radial J com páginas de itens possuídos, paginação6+2, mouse/voltar e uso pelo inventário existente; GPS de serviços reais/ativos, grupos de trabalhos e marcadores pessoais com renomear/confirmar exclusão. Gestos curtos de mãos para cima, sem limpar tarefas globais. F2/teleporte/saves/configs preservados.3725 testes aprovados; GTA0.97 pendente. Usuário confirmou0.96 funcionando e a revisão integral de todos os sistemas/menus continua no escopo.

## Atualização 0.96.0 — lojas por categorias e consumíveis sem apresentação duplicada

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.96.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.96.0.zip) · [Uso e testes GTA](docs/LOJAS-E-CONSUMIVEIS-0.96.0.md)

Conveniências e pontos Grocery passam à grade6x4 da referência, com mochila à esquerda, categorias, mouse/teclado/controle, quantidade1–100, total e limite pelo dinheiro/peso. Tab alterna categoria, +/- ajusta, M calcula máximo e Enter confirma; clicar no produto apenas seleciona. Preços, dinheiro, arquivos e coordenadas anteriores preservados. Removido o apresentador duplicado de consumíveis; um único fluxo usa seus modelos/offsets configurados e limpeza identificada.3686 testes aprovados; build sem erros/avisos. Reinicie o GTA uma vez para o teste dos props antigos sem identificação; roteiro completo no guia.

## Atualização 0.95.0 — correções do inventário e itens no chão

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.95.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.95.0.zip) · [Uso e testes GTA](docs/CORRECOES-INVENTARIO-0.95.0.md)

Limpeza de consumíveis com desanexação, confirmação e nova tentativa; recuperação de sobras próprias após reload. Bloqueio da roda separado da sincronização de armas e aplicado por frame mesmo em atividades a pé. Delete/LB agora largam unidade/pilha com posição e quantidade salvas; perto da marca azul, abra o inventário e use R/Shift+R para recolher unidade/o que couber. Preserva armas, munição, metadados, saves e arquivos instalados. Mouse e sobrepeso mantidos. Veja o guia para os limites e a validação no jogo.

## Atualização0.94.0 — mouse e peso funcional

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.94.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.94.0.zip) · [Uso e teste](docs/INVENTARIO-MOUSE-0.94.0.md)

Clique para selecionar; segure o botão esquerdo, arraste e solte para mover a pilha ou trocar com outro slot da mochila. Cursor/ícone de arraste, roda para rolar e botão direito para cancelar; posições salvas pelo caminho existente, quantidades preservadas. Teclado/controle continuam disponíveis; arrastar entre mochila e depósito cancela, transferências continuam por Enter/Shift+Enter.3602 testes aprovados; Release sem erros/avisos. Sobrecarga reduz movimento progressivamente e bloqueia sprint/salto a pé; liberar peso remove a restrição. Ctrl+Enter transfere o que cabe no depósito. Cursor/gestos e locomoção precisam teste no GTA.

## Atualização0.93.0 — inventário: atalhos e revisão agrupada

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.93.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.93.0.zip) · [Comparação completa e testes GTA](docs/INVENTARIO-REVISAO-0.93.0.md)

Teclas1..6 acima das letras usam os seis primeiros slots, sem repetição por tecla mantida ou uso por baixo de modais. Rolagem6x4 por linhas, numeração/peso, depósito/porta-malas/apartamento lado a lado com unidade/pilha, correção de slots antigos,63 apresentações de armas e31 novos itens sem sobrescrever19 IDs existentes. Nove receitas pessoais de shaker/lavagem; consumo6,5s com animação/prop próprio, retorno de recipientes e energia idempotente sem desfazer repouso. Compras gravadas imediatamente com rollback de falha normal.3541 testes aprovados; Release sem erros/avisos. Roteiro GTA pendente e diferenças restantes de ferramentas/intoxicação/atividades explicitadas no guia; não declara paridade integral da PSRP.

## Atualização 0.92.0 — armas e munição no inventário

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.92.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.92.0.zip) · [Como usar e testar](docs/REFINAMENTO-ARMAS-0.92.0.md)

Baseado nos fluxos da PSRP.dll: armas atuais importadas para o inventário, ENTER equipa/guarda sem consumir a arma ou conceder balas; munição separada por tipo nativo e arremessáveis como pilhas. Desconto pela variação real da munição; acessórios e pintura registrados no armazenamento. Roda oculta por padrão a pé em livre exploração, opção em F11 → Game Settings. Atividades, veículos e missões mantêm seus controles/equipamentos. 3482 testes aprovados e Release sem erros/avisos; testes físicos GTA pendentes. Todo próximo refinamento seguirá a comparação com a PSRP, corrigindo os problemas identificados.

## Atualização0.91.0 — correção do desmaio de sono

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.91.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.91.0.zip) · [Teste no GTA](docs/CORRECAO-SONO-0.91.0.md)

Corrige quedas repetidas da0.90: sono zerado causa uma recuperação de10s a pé/seguro; acorda com sono e fôlego100. Veículos/embarque protegidos, sem ejetar ou travar controles; episódio/tempo restantes persistidos. Cama nativa do Franklin reconhecida por contexto+salto do relógio; comida pendente não reverte descanso.3449 testes aprovados, build sem erros/avisos. Física e reconhecimento da cama precisam teste GTA. Correção pontual antes de continuar refinamento8.

## Atualização0.90.0 — fase8: roleplay pronto, necessidades e menus

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.90.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.90.0.zip) · [Guia de teste](docs/FASE-8.1-0.90.0.md)

RP por perfil já ativo quando nao ha atividade ativa, sem sequência de liga/desliga. F11 nas oito categorias PSRP; F10 separado em serviços/banco/perfis/manutenção. Teleporte em F11CheatMenu e testes/opções técnicas no avançado. Fome/sede/sono, fôlego ligado ao sono/oxigênio, avisosFitness, consumo persistido antes de benefícios, grade6x4 PSRP com itens/posições preservados; rollback de banco e proteção de hotkeys na troca de personagem.3415 testes aprovados; build sem erros/avisos. Guia detalha diferenças e teste GTA pendente. Fase8 continua para menus específicos/detalhes; não declara todo bug eliminado ou paridade visual total.

## Atualização0.89.0 — resolução policial e fechamento da base

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.89.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.89.0.zip) · [Guia de teste](docs/FASE-7.6-0.89.0.md)

F11Polícia: suborno opcional durante inspeção, uma tentativa salva/cobrança bancária só se aceito; BOLO opcional por fuga/rejeição, modelo+placa e regularização após resolver autuações de origem. Até2estrelas na rejeição só com perseguição/reforço habilitados, sem replay/limpar procurado. Co-líder configurado substitui LeaderModel ausente nos contatos/iniciações existentes.3372 testes aprovados; build sem erros/avisos. GTA pendente. [Fechamento da base](docs/FECHAMENTO-BASE-0.89.0.md): variantes físicas seguem9; LSPDFR opcional não implementado. **Próxima entrega: fase8, interfaces e mecânicas de necessidades.** Sem declarar paridade total PSRP.

## Atualização0.88.0 — táxi/limousine de passageiro e mundo

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.88.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.88.0.zip) · [Guia de teste](docs/FASE-7.5-0.88.0.md)

F2Contacts/F11/F10 → Transporte: contratar motorista, embarcar, viajar ao waypoint, mudar destino/velocidade, encerrar e receber o saldo não gasto da reserva bancária. Táxi/limousine/modelo visual próprio, taxímetro coerente, histórico e recuperação sem duplicar devoluções. F11/F10 → Mundo: clima manual/dinâmico/real, coordenadas/C-F, horário manual/sincronização local; tudo desativado por padrão nos saves antigos. Consulta meteorológica assíncrona com timeout/fallback; online precisa teste no GTA (proxy daqui bloqueou HTTP).3340 testes aprovados; build sem erros/avisos. IA, clima e integração física precisam do roteiro GTA. Diferenças restantes de gangues/polícia seguem no fechamento anterior aos refinamentos8/9.

## Atualização0.87.0 — personalização de veículos

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.87.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.87.0.zip) · [Guia de teste](docs/FASE-7.4-0.87.0.md)

Garagem comprada ou oficina do editor → Personalizar veículo: peças suportadas pelo modelo, desempenho, rodas, pintura/interior/perolado, xenon, neon/fumaça, extras, vidros/estilos de placa e proteção de pneus. Compra pelo banco após verificação, sem alteração ao navegar; snapshot dos carros cadastrados salvo com a compra, rollback de falhas normais.3255 testes aprovados; Release sem erros/avisos. Teste físico GTA pendente. Revisão confirma táxi/limousine de passageiro e clima/tempo como próximos blocos funcionais, antes dos refinamentos8/9.

## Atualização0.86.0 — Map Builder persistente

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.86.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.86.0.zip) · [Guia de teste](docs/FASE-7.3-0.86.0.md)

F11 → Map Builder: props próprios/transformações/chão/duplicação/remoção, ícones e pontos de loja/combustível/reparo-lavagem. Rascunho com salvar/cancelar, biblioteca por perfil, carregar/descarregar/auto-carregar opcional e importar/exportarJSON. Arquivos originais e props/ícones ambiente preservados; arquivo corrompido/alterado externamente protegido.3229 testes aprovados, build sem erros/avisos. GTA precisa de teste; tuning completo não é substituído pelo reparo/lavagem do editor. Próxima etapa: revisão final de paridade antes dos refinamentos8/9.

## Atualização0.85.0 — Halloween / The Last Ride

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.85.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.85.0.zip) · [Guia de teste](docs/FASE-7.2-0.85.0.md)

F11 → Halloween, GPS/marcador de fantasma: dirija oRomero até cemitério em180s, com perseguidores próprios e pulsoE/polegar direito a cada8s. Sustos opcionais usamJumpScareOne e áudio já instalados; não toma trânsito ambiente nem muda clima/relogio globais. Histórico/resultado/pulso salvos, recarga interrompe sem restaurar carro reparado; sem pagamento, conformePSRP.3193 testes aprovados; build sem erros/avisos. GTA precisa de teste; assalto ao estúdio/F2/F11/saves/visuais mantidos. Editor e revisão final são os próximos blocos antes dos refinamentos8/9.

## Atualização0.84.0 — The Hollywood Act completo em seis atos

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.84.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.84.0.zip) · [Guia dos seis atos](docs/FASE-7.1-0.84.0.md)

F11 → Contratos / assalto ao estúdio ou F2 → Contatos → Lester: suprimentos, documentos/noturno/fechadura, carro, Bison/reboque, bolsa de distração e final com infiltração/diretor/chave/dispositivo/fuga/barco/comprador. Preparações salvas, prazo/snapshot/retomada do ato, entrega pronta e prêmio bancário único$200–450mil. Recursos próprios/cleanup protegido, sem supressão global de polícia.3157 testes aprovados, build sem erros/avisos. GTA precisa de teste dos seis atos; Halloween/editor/revisão final seguem na fase7. F2/F11/saves/visuais e refinamentos8/9 preservados.

## Atualização0.83.0 — perseguição própria e quatro infrações adicionais

[Baixar atualização](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.83.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.83.0.zip) · [Guia de teste](docs/FASE-6.6C-0.83.0.md)

F11 → Polícia/trânsito: celular nativo, distância insegura, arma apontada e buzina prolongada; perseguição de carro/a pé e reforço nativo por fuga. Duas opções novas desligadas em saves antigos. Esperar parado não acumula fuga; pedido de estrelas gravado uma vez, respeita máximo do jogo e não reaplica após recarga. Onze checks ativosPSRP cobertos com filtros conservadores; sem suborno/BOLO automático/LSPDFR. Multas/custódia/saves/F2/F11 preservados.3111 testes aprovados; build sem erros/avisos, GTA precisa de teste. Próximo bloco fase7; refinamentos8/9 reservados.

## Atualização0.82.0 — custódia, pena e fiança

[Baixar DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.82.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.82.0.zip) · [Roteiro de teste](docs/FASE-6.6B-0.82.0.md)

F11 → Polícia/trânsito → Custódia: opção por perfil, rendição explícita durante perseguição nativa, policial a pé próximo/visão, fichamento, cela verificada, pena salva sem tempo offline, fiança bancária e liberação. Recarga exige retomada explícita; novas estrelas não são apagadas. Sem desligar polícia globalmente, confiscar inventário ou alterar multas anteriores.3070 testes aprovados; build sem erros/avisos. Interior Mission Row e reação da polícia precisam de teste no GTA. F2/F11/saves/configs/visuais mantidos. Fases8/9 reservadas.

## Atualização0.81.0 — fiscalização, abordagem e multas

[Baixar atualização0.81.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.81.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.81.0.zip) · [Roteiro único de teste](docs/FASE-6.6A-0.81.0.md)

F11 → Polícia/trânsito e multas (tambémF10):opção desligada em saves antigos, sete infrações reais/162limitesPSRP, testemunha50m/LOS e resposta própria. Patrulha de teste só multa se observar infração; encoste, permaneça ao volante e aguarde o agente. Autuação persistente sem débito automático; pagamento exato pelo banco, sem cobrança repetida. Polícia ambiente/Wanted/dispatch/armas do jogador preservados.3032testes aprovados e build sem erros/avisos; observação/condução/abordagem precisam do GTA. Não implementa prisão/perseguição ainda nem antecipa8/9; F2, teleporteF11 e sistemas anteriores mantidos.

## Atualização0.80.0 — emboscadas rivais e líderes

[Baixar atualização0.80.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.80.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.80.0.zip) · [Roteiro único de teste](docs/FASE-6.5AL-0.80.0.md)

F4 → Emboscadas rivais de carro:opção desligada nos saves antigos, teste real ou intervalo5–15min/85%, um motorista/três atiradores, aproximação/tiro/fuga e histórico sem prêmio. Fora de HQs/territórios e sem outra atividade. END/desligar interrompe; reload registra interrupção sem recriar ataque. E a1,5m do líder com presença ligada abre iniciação conforme tipo da gangue, retomada ou gestão/saída da própria gangue; outras afiliações não recrutam. Menus/saves/F2/F11 e recursos visuais preservados.2992 testes aprovados, build sem erros/avisos; tiros/navegação/conversa precisam do GTA. Polícia/prisão e fase7 seguem; refinamentos8/9 reservados.

## Atualização0.79.0 — assalto organizado com comboio

[Baixar atualização0.79.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.79.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.79.0.zip) · [Roteiro único de teste](docs/FASE-6.5AK-0.79.0.md)

F4 → Assalto organizado / comboio: a pé em território próprio, escolha alvo rival, dirija o líder com três escoltas e equipe de até15aliados, pare65m do alvo com uma escolta100m, desembarque e capture a pé com apoio. END/retomada conserva chegada, baixas e tempo; finalE concede apenas captura/+50rep existentes. Quatro carros próprios,60pontos/rumosPSRP, vagas alternativas sem apagar ambiente e limpeza protegida. Configs/saves/F2/F11/YTD/RPF mantidos.2931 testes aprovados e build sem erros/avisos; GTA precisa de teste. Drive-bys/diálogos e demais fases continuam;8/9 reservadas.

## Atualização 0.78.0 — guerras, diplomacia e reforços

[Baixar atualização 0.78.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.78.0.zip) · [Código-fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.78.0.zip) · [Roteiro único de teste](docs/FASE-6.5AJ-0.78.0.md) · [Pendências antes do refinamento](docs/PENDENCIAS-ANTES-REFINAMENTO.md)

F4: declaração de guerra na HQ, alertas/defesa dos seus territórios, simulação distante entre rivais e reforços nas novas capturas. Três opções desligadas em saves antigos; ignorar alerta habilitado por120segundos ativos transfere a área. Solicitar alerta agora permite testar sem esperar o intervalo, exigindo território próprio e rival hostil. Defesa e propriedade gravadas juntas, sem renda gratuita. F2/F11/saves/configs/YTD/RPF mantidos.2891 testes aprovados; build sem erros/avisos. GTA ainda precisa de teste. Comboios/assaltos organizados/drive-bys/diálogos restantes, polícia e fase7 seguem na fila agrupada; refinamentos8/9 preservados.

# RPCore 0.77.0 — GTA V Legacy

## Fase6.5AI — turno de caixa 24/7

[Atualização0.77.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.77.0.zip) · [Fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.77.0.zip) · [Guia/teste](docs/FASE-6.5AI-0.77.0.md)

F4, território15: clientes compram nas prateleiras, E registra cada item e recebe pagamento. Cinco clientes em10min, E final paga$400 em dinheiro pessoal; sem bônus de processamento, conforme código real da PSRP. Atendente original preservado, pode trabalhar pelo lado externo. Cestas/scans/tempo persistem.2832 testes aprovados; GTA requer teste do usuário. Fases8/9 permanecem reservadas.

## Fase6.5AH — manutenção Del Perro Pier

[Atualização0.76.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.76.0.zip) · [Fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.76.0.zip) · [Guia/teste](docs/FASE-6.5AH-0.76.0.md)

F4, território14: E a pé em21 pontos da PSRP,2 reposições+19 regas,3s por ação antes do prazo5min. E final concede processamento2X por1h. Tarefas/tempo persistem ao suspender. Sem pagamento pessoal.2797 testes aprovados; GTA requer teste do usuário. Fases8/9 permanecem reservadas.

## Fase6.5AG — entrega em alto-mar

[Atualização0.75.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.75.0.zip) · [Fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.75.0.zip) · [Guia/teste](docs/FASE-6.5AG-0.75.0.md)

F4, território13: E coleta pacote, use Seashark próprio e pare próximo do comprador no Squalo por5s antes do prazo5min. E final concede processamento2X por1h. Coleta/tempo/entrega persistem, retomada em terra. Sem pagamento pessoal.2759 testes aprovados; GTA requer teste do usuário. Fases8/9 permanecem reservadas.

## Fase6.5AF — circuito promocional BMX

[Atualização0.74.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.74.0.zip) · [Fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.74.0.zip) · [Guia/teste](docs/FASE-6.5AF-0.74.0.md)

F4, território12: use a BMX fornecida para passar pelos11 checkpoints da PSRP em75s ativos. Progresso/tempo persistem ao suspender. E final concede processamento2X por1h; sem pagamento pessoal.2726 testes aprovados; GTA requer teste do usuário. Fases8/9 permanecem reservadas.

## Fase6.5AE — frete do porto

[Atualização0.73.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.73.0.zip) · [Fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.73.0.zip) · [Guia/teste](docs/FASE-6.5AE-0.73.0.md)

F4, território11: retire Phantom/trflat, engate, carregue7 paletes na baia(3s cada), entregue e descarregue(2s cada) no prazo10min. E final concede processamento1,25X/1,5X/2X conforme condição da PSRP. Progresso/tempo/danos persistem ao suspender; sem pagamento pessoal.2688 testes aprovados; GTA requer teste do usuário. Fases8/9 permanecem reservadas.

## Fase6.5AD — motorista particular VIP

[Atualização0.72.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.72.0.zip) · [Fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.72.0.zip) · [Guia/teste](docs/FASE-6.5AD-0.72.0.md)

F4, território10: retire carro de luxo, busque VIP e entregue no prazo, com conforto e sem danos. Sete carros/15 destinos/três perfis da PSRP. Avaliação dá processamento1,25X/1,5X/2X por1h; nota/tempo preservados ao suspender. Sem pagamento pessoal.2640 testes aprovados; GTA requer teste do usuário. Fases8/9 permanecem reservadas.

## Fase6.5AC — estreia Oriental Theatre

[Atualização0.71.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.71.0.zip) · [Fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.71.0.zip) · [Guia/teste](docs/FASE-6.5AC-0.71.0.md)

F4, território9: E coleta equipamento na caixa, transporte a pé e E instala cada uma das11 peças da PSRP. Coleta/instalações persistem ao suspender/recarregar. E final solicita tapete vermelho por10min de jogo e libera processamento2X por1h. Cenário encontrado ativo não é removido por esta atividade.2588 testes aprovados; GTA requer teste do usuário. Fases8/9 permanecem reservadas.

## Fase6.5AB — jardinagem Mirror Park

[Atualização0.70.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.70.0.zip) · [Fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.70.0.zip) · [Guia/teste](docs/FASE-6.5AB-0.70.0.md)

F4, território8: dirija o cortador fornecido sobre57 pontos da PSRP. Cortes persistem ao suspender/recarregar; retomada conserva os pontos concluídos. Após todos, saia e E perto do cortador parado ou no início confirma processamento2X por1h. Limpeza preserva cortador ocupado/movendo.2545 testes aprovados; GTA requer teste do usuário. Fases8/9 permanecem reservadas.

## Fase6.5AA — loja temporária Legion

[Atualização0.69.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.69.0.zip) · [Fonte](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.69.0.zip) · [Guia/teste](docs/FASE-6.5AA-0.69.0.md)

F4, território7: clientes pedem peça e cupom; setas selecionam e E confirma/para a barra. Cada acerto gera receita suja local;10 vendas e confirmação final liberam2X por1h. Vendas/pedido persistem ao suspender; cancelamento conserva receita confirmada.2510 testes aprovados; GTA requer teste do usuário. Fases8/9 permanecem futuras.

## Fase6.5Z — documentos médicos

[Atualização0.68.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.68.0.zip) · [Código-fonte0.68.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.68.0.zip) · [Instalação e teste](docs/FASE-6.5Z-0.68.0.md)

F4 → Comercio, território6 controlado: GPS médico → a pé/ponto livre → Entregar documentos medicos. E<=2m/troca1,5s coleta com médico/prancheta/papel na mão. Viaje a pé ou em veículo; papel oculto no carro/visível ao sair. E<=2m no centro médico salva entrega, E final2XUTC60min área6. Coleta preservada emEND/reload, retoma a pé sem refazer médico; entregue confirma sem respawn. Livro/snapshot/recibos/rollback/cancelamento grátis, sem tocar carro pessoal/estoque/dinheiro ou auto-enable. Props respeitam preferências. Máximo2X/85%/produção ativa intactos.2468 testes/build sem erros ou avisos; gameplay pendente. Loja temporária/demais atividades/refinamentos/diálogo/guerras/polícia e fases8/9 futuros.

## Fase6.5Y — transporte de valores Gruppe

[Atualização0.67.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.67.0.zip) · [Código-fonte0.67.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.67.0.zip) · [Instalação e teste](docs/FASE-6.5Y-0.67.0.md)

F4 → Comercio, território5 controlado: GPS Stockade → a pé/vaga livre/afaste-se4m → Transportar valores /5 malotes. Blindado fornecido, segurança mais próximo, E recebe bag/1,5s e E traseira carrega/props visíveis respeitando preferências. Cinco carregados, dirigir ao depósito/parar/sair, E recebedor/1,5s salva entrega e E final2XUTC60min na área5; sem crédito de dinheiro/estoque/auto-enable. Parcial refaz coletas/snapshot mantido, entregue retoma sem respawn; recibos/rollback/cancelamento grátis. Recebedor assume blindado/sai, cleanup próprio/ocupados protegidos. Bônus máximo2X/85% ao caixa/produção ativa.2428 testes aprovados/build sem erros ou avisos; gameplay pendente. Demais trabalhos/refinamentos/diálogo/guerras/polícia e fases8/9 futuros.

## Fase6.5X — recuperação com guincho territorial

[Atualização0.66.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.66.0.zip) · [Código-fonte0.66.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.66.0.zip) · [Instalação e teste](docs/FASE-6.5X-0.66.0.md)

F4 → Comercio, território3 controlado: GPS pátio → a pé/vaga livre → Recuperar veiculos /2 chamados. Guincho fornecido, NUM8/NUM2 braço, E engata carro próprio da missão, E parado no pátio descarrega/assenta. Duas entregas salvas e E final a pé liberam2X/60minUTC na área3, sem pagamento/estoque/auto-enable. Dados126 carros/24 clientes/15 locais PSRP; segue lógica2 apesar da notificação3 da fonte. Grade de dano informativa, soltura acidental não entrega, retomada conserva chamado/opções/entregas. Limpeza própria vazia/parada/sem ligação externa, sem ejetar/encerrar scripts GTA; H preservado. Livro/recibos/rollback/cancelamento grátis, bônus máximo2X/85% ao caixa/sem renda offline.2388 testes aprovados/build sem erros ou avisos; física e convivência no GTA pendentes. Demais sistemas/refinamentos/fases8/9 futuros.

## Fase6.5W — contratos da oficina territorial

[Atualização0.65.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.65.0.zip) · [Código-fonte0.65.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.65.0.zip) · [Instalação e teste](docs/FASE-6.5W-0.65.0.md)

F4 → Comercio, território2 controlado: GPS oficina → a pé/vaga livre → Atender oficina /3 carros. Cliente próprio chega dirigindo danificado; E na frente abre capô, acerte3 vezes a faixa verde. Reparo salvo/cliente sai/próximo carro. Após3, E final na oficina libera2X/60minUTC na área2, sem pagamento/estoque/auto-enable. Catálogo120/pontos/dano/animação/minigamePSRP, sem teleporte/deletar carro pessoal. Retomada conserva carros concluídos/modelos/opções e refaz só reparo em andamento; pronto retoma sem respawn. Livro/recibos/rollback/cancelamento grátis; máximo2X com café/tatuagem,85% ao caixa e produção ativa/sem offline. 2348 testes aprovados e build sem erros/avisos; gameplay pendente. Demais trabalhos/refinamentos/diálogo/guerras/polícia e fases8/9 futuros.

## Fase6.5V — divulgação da loja de tatuagem

[Atualização0.64.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.64.0.zip) · [Código-fonte0.64.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.64.0.zip) · [Instalação e teste](docs/FASE-6.5V-0.64.0.md)

F4 → Comercio, território1 controlado: GPS da loja → a pé → Divulgar loja /5 panfletos. Marcação de pedestres civis reais na faixa50–120m; E a até2,2m/1,5s ativos para cada cliente diferente. Cinco entregas e retorno/E na loja liberam2X por60minUTC somente na área1. Grátis, sem estoque/renda/reputação e sem ligar automático; padrão500/30s ativos/425 ao caixa quando houver saldo sujo. Livro opcional, snapshot/recibos/rollback, parcial refaz clientes ao retomar/pronto preservado/cancelamento grátis. Não modifica/apaga NPCs ambiente. Café preservado/máximo2X/sem renda offline. 2309 testes aprovados, build sem erros/avisos; teste no GTA pendente. Demais trabalhos territoriais/refinamentos/diálogo/guerras/polícia e fases8/9 futuros.

## Fase 6.5U — café e processamento2X

[Atualização0.63.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.63.0.zip) · [Código-fonte0.63.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.63.0.zip) · [Instalação e teste](docs/FASE-6.5U-0.63.0.md)

F4 → Comercio, com posse do território4: GPS do pátio → seu carro/van → Abastecer cafe. Três coletas/carregamentos, retorno ao pátio, três entregas na porta e E final liberam2X por60min reais. Sem custo/pagamento/estoque ao concluir; não liga automático sozinho. Com receita suja e automático ativo na área4, lote500 passa de60s para30s ativos,85% ao caixa igual. ExpiraUTC inclusive offline, sem produzir offline; outra rota completa renova sem somar duração, replay não renova. Contrato e bônus atômicos/rollback, parcial/pronto/retomada/cancelamento e dados legados preservados. 2270 testes aprovados, build sem erros/avisos; gameplay pendente. Usa carro emprestado, sem van fornecida/três props no veículo/MissionWin; demais sistemas e fases8/9 futuros.

## Fase 6.5T — processamento automático da receita suja

[Atualização 0.62.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.62.0.zip) · [Código-fonte 0.62.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.62.0.zip) · [Instalação e teste](docs/FASE-6.5T-0.62.0.md)

F4 → Comercio → Processamento automatico por área, inicialmente desligado. Até$500 sujos a cada60s ativos geram85% ao caixa, mantendo arredondamento cumulativo junto do modo manual. AutoLaunder+Illicit+Treasury em uma Wallet, rollback/replay e histórico por perfil/gangue/área; sem banco/cash ou renda offline. Menu/pausa congela, atividade/contratos/risco/saldo zero reiniciam intervalo; stop após perder posse. Automatizações de venda de armas e processamento são independentes e compatíveis. 2236 testes aprovados, build sem erros/avisos; gameplay pendente. Saves/configs/F2/F11 preservados. Café/boost2X e demais diferenças/refinamentos/fases8/9 continuam futuros.

## Fase 6.5S — venda automática de armas

[Atualização 0.61.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.61.0.zip) · [Código-fonte 0.61.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.61.0.zip) · [Instalação e teste](docs/FASE-6.5S-0.61.0.md)

F4 → Comercio → Arsenal / armas: opção Auto por categoria, inicialmente desligada. Uma arma selecionada/com estoque a cada180s ativos por área gera$500 de receita suja; munição excluída. Compra padrão de pistola$900 e entrega física$1200 continuam separadas do preço automático fixo. Livro/estoque/receita atômicos, rollback/replay e histórico por perfil/gangue/área. Menu/pausa congela, atividade/contratos/risco/falta de estoque reiniciam intervalo; sem vendas offline. Controle para desligar mesmo após perder posse. 2206 testes aprovados, build sem erros/avisos; gameplay pendente. Saves/configs/F2/F11 preservados. Lavagem automática/boost e demais sistemas/refinamentos/fases8/9 continuam futuros.

## Fase 6.5R — roubo de van de armas

[Atualização 0.60.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.60.0.zip) · [Código-fonte 0.60.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.60.0.zip) · [Instalação e teste](docs/FASE-6.5R-0.60.0.md)

F4 → Comercio dos territorios → Roubar van de armas: missão gratuita, GBurrito2 própria parada com3 guardas, elimine os três e tome o volante. Volte dirigindo/parado≤15m da área sem procurado; saia/E≤3m confirma arsenal/Recovered. Carga base PSRP sorteada uma vez, sem dinheiro/armas pessoais/reputação ou relação. Retomada parcial refaz encontro com mesma carga; devolução pronta salva dispensa van. Kind0 legado/Kind1 van, crédito atômico Raid+Arms, conservação e limpeza só de entidades marcadas/van vazia e parada. 2172 testes aprovados, build sem erros/avisos; gameplay pendente. Convoys móveis/escala de carga/automação/refinamento e fases8/9 continuam futuros. Saves/configs/F2/F11 preservados.

## Fase 6.5Q — arsenal comercial e entrega de armas

[Atualização 0.59.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.59.0.zip) · [Código-fonte 0.59.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.59.0.zip) · [Instalação e teste](docs/FASE-6.5Q-0.59.0.md)

F4 → Comercio dos territorios → Arsenal / armas: cinco categorias comerciais separadas de drogas e armas pessoais. Compra própria paga no banco, a pé na área; entrega física no seu carro com caixa/traseira/viagem/comprador3s/E final usa estoque de armas e gera receita suja. Preços de venda PSRP1200/2500/4500/3200/3, quantidades1/5/10/25/50 e10 vagas por área (munição por100), sem bônus diplomático de drogas. Slot compartilhado com Kind0 legado/Kind1 armas, ready/retomada/cancelamento/rollback, sem equipar/retirar armas nativas. 2133 testes aprovados, build sem erros/avisos; teste GTA pendente. Roubo de van/convoys/automação/emboscada/dano e demais diferenças PSRP e fases8/9 continuam futuras. Saves/configs/F2/F11 preservados.

## Fase 6.5P — venda em lote entre gangues

[Atualização 0.58.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.58.0.zip) · [Código-fonte 0.58.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.58.0.zip) · [Instalação e teste](docs/FASE-6.5P-0.58.0.md)

F4 → Comercio dos territorios → Venda em lote: escolha produto/quantidade e gangue compradora. E pega/carrega caixa, dirija seu carro ao encontro, E retira/entrega por3s e E final confirma venda. Só então desconta armazém e credita receita suja, com30% de chance salva de melhorar relação. Quantidades25/50/100/200/500,150% do preço Dealer/teto$75000, seis destinos PSRP. Lote comprometido bloqueia operações territoriais até concluir/cancelar. END/reload preservam snapshot; pronta retoma a pé no encontro. Um comprador próprio; sem van fornecida/crew/emboscada/dano no preço nesta base. 2091 testes aprovados, build sem erros/avisos; teste no GTA pendente. Preserva saves/configs, F2/F11 e fases8/9 futuras.

## Fase 6.5O — relações e acordos

[Atualização 0.57.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.57.0.zip) · [Código-fonte 0.57.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.57.0.zip) · [Instalação e teste](docs/FASE-6.5O-0.57.0.md)

F4 → Relacoes / acordos: consulta por perfil/par de gangues e negociação presencial na sua HQ. A revista de carga rival piora a relação uma vez, seguindo Respeito → Neutro → Hostilidade → Guerra; pacote e consequência são salvos juntos. Acordo próprio do RPCore custa $1000 do banco por nível melhor e exige intervalo de 24h do jogo por par. Sem cobrança em Respeito, débito repetido ou recompensa/estoque. Guerra é estado diplomático, sem ataques automáticos. Saves/configs e F2/F11 preservados. 2047 testes aprovados, build sem erros/avisos; teste no GTA pendente. Venda em lote, guerras/convoys/armas/boost/diálogo/polícia e fases 8/9 seguem pendentes.

## Fase6.5N — roubo de suprimentos rivais

[Atualização0.56.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.56.0.zip) · [Código-fonte0.56.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.56.0.zip) · [Instalação e teste](docs/FASE-6.5N-0.56.0.md)

F4 → Comercio dos territorios → Roubar suprimentos: missão gratuita em área própria, um traficante e dois guardas hostis próprios, revista E por2s após os três abates com prova do jogador, retorno a pé e sem procurado para E entregar dois produtos ao armazém. Padrão35–90/un por SKU, sem dinheiro/estoque antes da entrega. NPCs com token de propriedade, até3; polícia nativa e veículos do jogador preservados. Suspensão/reload parcial refaz encontro sem rerrolar carga, carga recuperada fica salva; morte/abandono/prazo falham. Raided separado no histórico, contas/recibos/rollback e bloqueio de operações territoriais enquanto pendente. 2002 testes aprovados, build sem erros/avisos. Fretes pagos e saves anteriores mantidos. Guerras automáticas/convoys/armas/boost/diálogo/polícia e fases8/9 seguem pendentes.

## Fase6.5M — frete para o armazém criminal

[Atualização0.55.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.55.0.zip) · [Código-fonte0.55.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.55.0.zip) · [Instalação e teste](docs/FASE-6.5M-0.55.0.md)

No armazém criminal do F4, escolha Frete criminal do produto desejado a bordo de seu carro/van parado na área. Lote padrão10 pacotes com10% desconto sobre preço de compra Dealer (maconha$360). Três coletas/carregamentos, retorno dirigindo, três descargas e E final abastecem só o armazém criminal. Mesma reserva persistente/retomada/estorno do frete legal, sem cobrar duas vezes; estoque pessoal e mercadorias legais não recebem esse lote. Supplied separado no histórico do armazém; receita só nasce de vendas posteriores. 1967 testes aprovados e build sem erros/avisos. Preserva saves/configs/F2/F11. Rota paga própria; roubo de rivais/combate e demais diferenças PSRP continuam pendentes, assim como fases8/9.

## Fase6.5L — armazém criminal territorial

[Atualização0.54.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.54.0.zip) · [Código-fonte0.54.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.54.0.zip) · [Instalação e teste](docs/FASE-6.5L-0.54.0.md)

F4 → Comercio dos territorios → Armazem criminal: guardar/retirar1 ou5 pacotes entre estoque pessoal do tráfico e estoque local, sem dinheiro. Capacidade30kg e999/un por produto, com peso pessoal respeitado ao retirar. Vender do armazem usa apenas estoque da área, mantendo pacotes pessoais; receita suja alimenta processamento85% e tesouraria existentes. Modo bancário e venda territorial do estoque pessoal permanecem disponíveis. Contas por gangue/área/perfil, conservação/recibos/rollback e perda de posse sem apagar estoque. 1934 testes aprovados, build sem erros/avisos; saves/configs/F2/F11 preservados. Logística criminal/armas/automação, café/boost, guerras/diálogo/polícia e fases8/9 seguem pendentes.

## Fase 6.5K — vendas territoriais e processamento de receita

[Atualização 0.53.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.53.0.zip) · [Código-fonte 0.53.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.53.0.zip) · [Instalação e teste](docs/FASE-6.5K-0.53.0.md)

F4 → Comercio dos territorios → Vendas ilegais usa pacotes/compradores/negociação/riscos do tráfico existente, com receita suja isolada por gangue/área/perfil. Modo normal do tráfico continua pagando no banco. No território, processar até500 por60s ativos e E transfere85% ao caixa e contabiliza taxa15%, com arredondamento cumulativo. Sem lavagem offline ou conversão de receitas antigas. Venda e processamento usam uma gravação Wallet com recibos/rollback; receita processada financia tesouraria/participação existentes. Estoque legal, fretes, configurações, saves, F2 e F11 mantidos. 1901 testes aprovados, build sem erros/avisos. Estoque criminal específico por área, automação, café/boost, guerras/diálogo/polícia e fases8/9 seguem pendentes.

## Fase 6.5J — frete de reposição territorial

[Atualização 0.52.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.52.0.zip) · [Código-fonte 0.52.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.52.0.zip) · [Instalação e teste](docs/FASE-6.5J-0.52.0.md)

F4 → Comercio dos territorios → Frete: reserve10 unidades por $150 (25% desconto), usando seu carro/van parado no território. GPS para três fornecedores da referência; E pega caixa e guarda pela traseira, depois dirija de volta e descarregue três caixas a pé. E final acrescenta estoque uma vez, sem nova cobrança. END suspende; reserva paga fica salva para retomar ou estornar explicitamente. Retomada parcial refaz carga sem novo débito, entrega pronta persiste. Não apaga/move/repara seu veículo, respeita props visuais existentes. Compra direta/atendimento/tesouraria anteriores mantidos; frete pendente bloqueia novas compras/vendas até resolver. 1867 testes aprovados e build sem erros/avisos. Logística/posições/props precisam do GTA; não inclui bônus de lavagem do café PSRP. Fases8/9 mantidas.

## Fase 6.5I — comércio dos territórios

[Atualização 0.51.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.51.0.zip) · [Código-fonte 0.51.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.51.0.zip) · [Instalação e teste](docs/FASE-6.5I-0.51.0.md)

F4 → Comercio dos territorios: estoque por gangue/área/perfil, compra bancária de 10 unidades por $200 e atendimento presencial de 45 segundos para vender 5 por $175 ao caixa da gangue. E confirma; END encerra sem consumir mercadoria. Pausa/menu não contam; sem vendas offline. Receita financia retirada/participação existentes na tesouraria. Compra/venda atômicas na carteira, limites/recibos/rollback, saves anteriores conservados. 1833 testes aprovados, build sem erros/avisos. Comércio legal próprio e ativo nesta etapa; reposição com veículo, compradores, comércio criminal/lavagem e guerras automáticas continuam pendentes. Fases 8/9 mantidas.

## Fase 6.5H — progressão e tesouraria da gangue

[Atualização 0.50.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.50.0.zip) · [Código-fonte 0.50.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.50.0.zip) · [Instalação e teste](docs/FASE-6.5H-0.50.0.md)

F4 mostra patentes250/750/1500, sem teto100; captura+50, defesa+25, perda−20 salvos junto ao resultado. F4 → Tesouraria permite consulta/GPS e depósito/retirada presencial na HQ. Caixa financiado explicitamente pelo banco, participação manual de 5/10/20/35% após24h do jogo, sem criar dinheiro. Contas/recibos/rollback por perfil e gangue; saves/configs anteriores mantidos. 1803 testes aprovados e build sem erros/avisos. Calendário/HQ/banco precisam de GTA; comércio/renda de territórios/salário automático/guerra/polícia e fases 8/9 seguem.


## Fase 6.5G — defesa de territórios

[Atualização 0.49.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.49.0.zip) · [Código-fonte 0.49.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.49.0.zip) · [Instalação e teste](docs/FASE-6.5G-0.49.0.md)

F4 → Territorios → Defender área própria: padrão duas ondas de três atacantes, confirmação E da vitória e contadores de defesa/perda. Morte/veículo/abandono/timeout em defesa ativa transferem posse ao atacante, com aviso no menu; suspender/cancelar/falha técnica conservam dono. Sem ataques automáticos nesta etapa. Save 0.48 continua Attack por padrão, capturas/configs/sistemas anteriores preservados. 1771 testes aprovados e build sem erros/avisos. Ondas/piso/IA precisam de GTA; guerra automática/economia/polícia e fases 8/9 seguem.


## Fase 6.5F — mapa e captura de territórios

[Atualização 0.48.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.48.0.zip) · [Código-fonte 0.48.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.48.0.zip) · [Instalação e teste](docs/FASE-6.5F-0.48.0.md)

F4 → Territorios / mapa e disputas → mapa ligado. Quinze centros/postos/donos da PSRP, GPS, combate explícito contra três defensores próprios e confirmação E da posse após vencer. Donos por perfil, mapa opcional, snapshots/retomada/rollback e atualização dos ícones. Sem renda/recompensa/captura automática, polícia ou mudanças de grupos nativos. Sistemas/saves/configs anteriores preservados. 1744 testes aprovados e build sem erros/avisos. Combate/piso/acesso precisam de GTA; reforços/guerras/economia/polícia e fases 8/9 seguem.


## Fase 6.5E — iniciação de direção

[Atualização 0.47.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.47.0.zip) · [Código-fonte 0.47.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.47.0.zip) · [Instalação e teste](docs/FASE-6.5E-0.47.0.md)

F4 → presença ligada → sem filiação, perto do líder → Iniciacao por direcao. Integrante embarca no banco dianteiro, jogador dirige até alvo rival em um de 25 pontos PSRP, só morte pelo integrante aprova ataque, fuga150m e retorno juntos na HQ. Aprovação salva, E no líder e retomada sem tentativa/taxa extra; carro do jogador e dados antigos preservados. 1720 testes aprovados e build sem erros/avisos. Embarque/tiro pela janela/autoria/piso precisam de GTA. Diálogo/territórios/guerras/economia/polícia e fases 8/9 seguem.


## Fase 6.5D — iniciação por força

[Atualização 0.46.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.46.0.zip) · [Código-fonte 0.46.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.46.0.zip) · [Instalação e teste](docs/FASE-6.5D-0.46.0.md)

F4 → presença ligada → sem filiação, perto do líder e com punhos → Iniciacao por forca. Três desafiantes preparados antes de iniciar, combate físico com regras/dano e derrotas distintas, aprovação salva e E no líder para filiação única. Suspender luta parcial exige vencer três novos na retomada, sem tentativa/taxa extra; aprovação completa permanece emitível após reload. Lealdade/presença/equipe/saves/configs/F2/F11 mantidos. 1683 testes aprovados e build sem erros/avisos. Luta/flags de dano/rendição precisam de GTA; direção/territórios/guerras/economia/polícia e fases 8/9 seguem.


## Fase 6.5C — iniciação por lealdade

[Atualização 0.45.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.45.0.zip) · [Código-fonte 0.45.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.45.0.zip) · [Instalação e teste](docs/FASE-6.5C-0.45.0.md)

F4 → presença ligada → sem filiação, perto do líder → Iniciacao por lealdade. Dois contatos diferentes entre 15 posições PSRP, E a pé/2,5s, confirmação persistente dos pacotes e volta ao líder para gravar filiação. Suspensão/retomada com recibos, sem taxa/recompensa ou cutscene forçada; trava compartilhada impede conflito com trabalhos/exames. Join anterior preservado sem pendência; presença/equipe/saves/configs/F2/F11 mantidos. 1650 testes aprovados e build sem erros/avisos. Contatos/piso/animação precisam de GTA; outras iniciações/territórios/guerras/economia/polícia e fases 8/9 seguem.


## Fase 6.5B — equipe da gangue / recrutamento e ordens

[Atualização 0.44.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.44.0.zip) · [Código-fonte 0.44.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.44.0.zip) · [Instalação e teste](docs/FASE-6.5B-0.44.0.md)

F4 → presença ligada → afilie-se → aproxime-se de um membro → Recrutar membro mais proximo. Até 3 integrantes próprios acompanham fora da HQ, com ordens Seguir/Aguardar/Dispensar e embarque físico em passageiros livres de carro parado. Sem líderes/rivais/NPCs alheios, grupo nativo, armas, teleporte ou embarque em veículos reservados de trabalho. Equipe só da sessão; filiação/reputação/saves/configurações anteriores preservados. 1617 testes aprovados e build sem erros/avisos. Navegação/portas/assentos precisam de GTA; iniciações/territórios/guerras/economia/polícia e fases 8/9 seguem.


## Fase 6.5A — presença de gangues nas HQs

[Atualização 0.43.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.43.0.zip) · [Código-fonte 0.43.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.43.0.zip) · [Instalação e teste](docs/FASE-6.5A-0.43.0.md)

F4 → Presenca nas HQs → LIGADA → GPS da gangue. Líder/membros nos postos existentes, limites 6/HQ e 12 no total, geração/retirada 80/160 m, retorno a pé, cooldown e limpeza própria. F4 também define líder/adiciona/remove postos da gangue criada no RPCore à qual está filiado. Preferência por perfil, desligada em saves antigos; definições/filiação/metadados e DMV C/M1/A preservados. 1575 testes aprovados e build sem erros/avisos. Modelos/chão/navegação precisam de GTA. Territórios/guerras/economia/polícia e demais diferenças continuam, com fases 8/9 mantidas.


## Fase 6.4D — DMV / habilitação A para caminhões

[Atualização 0.42.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.42.0.zip) · [Código-fonte 0.42.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.42.0.zip) · [Instalação e teste](docs/FASE-6.4D-0.42.0.md)

F10 → DMV → Trocar até A → GPS: DMV. Atendimento industrial próprio, teoria ($30/12 acertos), prático ($120) com Phantom/carreta/examinadora, engate manual, 16 pontos/paradas, ré de 5 m, alinhamento, desengate e emissão por perfil. Retomada conserva taxa/pontos/faltas/tempo e exige novas provas físicas no conjunto substituto. C/M1/saves/configs/F2/F11 preservados, sem novos bloqueios aos trabalhos. 1536 testes aprovados e build sem erros/avisos. Física, engate e rota precisam de GTA. Fiscalização, demais diferenças e fases 8/9 continuam.



## Fase 6.4C — DMV / habilitação M1 para motos

[Atualização 0.41.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.41.0.zip) · [Código-fonte 0.41.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.41.0.zip) · [Instalação e teste](docs/FASE-6.4C-0.41.0.md)

F10 → DMV / habilitacoes C e M1 → Trocar para M1: teoria própria ($30/12 acertos), prático PCJ 600/examinadora ($60), capacete/setas, três trechos a 15 km/h e demais a 40, paradas/estacionamento e emissão por perfil. Documentos/recibos/tentativas M1 separados da C, com retomada sem nova taxa; capacete do atendimento protegido contra gravação como roupa permanente. 1490 testes aprovados e build sem erros/avisos. C/saves/configs/F2/F11 preservados; física/capacete precisam de GTA. Classe A/fiscalização e fases 8/9 seguem.


## Fase 6.4B — DMV / habilitação classe C

[Atualização 0.40.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.40.0.zip) · [Código-fonte 0.40.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.40.0.zip) · [Instalação e teste](docs/FASE-6.4B-0.40.0.md)

F10 → DMV / habilitacao C: teoria própria de 15 questões ($30/12 acertos), prático com Asterope/examinadora ($60), setas/cinto, 36 pontos PSRP, paradas/avaliação/estacionamento e emissão por perfil. Retomada paga sem nova taxa, dados e recursos anteriores preservados. Não bloqueia os trabalhos existentes por falta de licença. 1450 testes aprovados e build sem erros/avisos. Percurso/atores precisam de GTA; classes A/M1/fiscalização e fases 8/9 seguem.


## Fase 6.4A — seguro de veículos pessoais

[Atualização 0.39.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.39.0.zip) · [Código-fonte 0.39.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.39.0.zip) · [Instalação e teste](docs/FASE-6.4A-0.39.0.md)

F10 → Seguro veicular / recuperacao: cadastro gratuito de carro/moto registrado na garagem, GPS, cancelamento e reparo do mesmo veículo destruído no local, com franquia padrão $10.000 do banco. Pedido persistente/retomada sem cobrar novamente, preservação de combustível/óleo e proteção da garagem enquanto pendente. 1412 testes aprovados e build sem erros/avisos. Saves/configs/personagens/F2/F11 preservados; reparo e aparência exigem GTA. Entrega/guincho/licenças e demais diferenças continuam; fases 8/9 mantidas.


## Fase 6.3E — criar e carregar personagens

[Atualização 0.38.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.38.0.zip) · [Código-fonte 0.38.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.38.0.zip) · [Instalação e teste](docs/FASE-6.3E-0.38.0.md)

F10 → Personagens / criar e carregar: nome, freemode masculino/feminino e perfil próprio de saldo zero. Confirmação antes de trocar, restauração de roupa/equipamento e aparência confirmada no RPCore, volta ao protagonista de origem e pendências protegidas. Nenhuma troca automática ao atualizar/reiniciar. Build sem erros/avisos e 1374 testes aprovados. Preserva saves/configs/F2/F11; modelos/equipamentos precisam de teste no GTA. Câmera/prévia completa e demais diferenças continuam; fases 8/9 mantidas.


## Fase 6.3D — herança e traços faciais

[Atualização 0.37.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.37.0.zip) · [Código-fonte 0.37.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.37.0.zip) · [Instalação e teste](docs/FASE-6.3D-0.37.0.md)

F10 → Barbearias / cortes e olhos → Aparencia / rosto e acabamento: herança de formato/pele e 20 traços independentes no freemode existente. Seleção sem alterar personagem; taxa única padrão $120 por conjunto e aplicação recuperável. Aguarda head blend em ticks antes dos detalhes, sem bloquear script; registros antigos mantidos. Build sem erros/avisos e 1332 testes aprovados. Saves/configs/F2/F11 preservados. Aparência exige teste no GTA; criação/troca de ped, tatuagens e fases 8/9 continuam pendentes.


## Fase 6.3C — pele e maquiagem

[Atualização 0.36.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.36.0.zip) · [Código-fonte 0.36.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.36.0.zip) · [Instalação e teste](docs/FASE-6.3C-0.36.0.md)

F10 → Barbearias / cortes e olhos → Acabamento / pele e maquiagem: 11 grupos de pele/maquiagem/corpo no freemode existente, com estilo, remoção, opacidade e paletas adequadas. Mesmo atendimento padrão $120, confirmação apenas dos grupos escolhidos, retomada gratuita e compatibilidade com tintura/barba da 0.35. 1267 testes aprovados e build sem erros/avisos. Saves/configs/F2/F11 preservados; aparência exige GTA. Criador facial completo e fases 8/9 continuam.

## Fase 6.3B — tintura, barba e sobrancelhas

[Atualização 0.35.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.35.0.zip) · [Código-fonte 0.35.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.35.0.zip) · [Instalação e teste](docs/FASE-6.3B-0.35.0.md)

Na barbearia: F10 → Barbearias → Tintura / barba / sobrancelhas. Freemode com rosto carregado, grupos independentes, paleta/estilo/opacidade e aplicação somente após confirmar $120 pelo banco. Cancelar rascunho não muda visual; pendência permite retomar sem nova cobrança. Sem prévia no personagem ou backup inventado das cores originais. 1233 testes aprovados e build sem erros/avisos; renderização precisa de teste no GTA. Saves/configs/sistemas anteriores e fases 8/9 preservados; criador facial e demais diferenças continuam.

## Fase 6.3A — barbearias / cortes e olhos

[Atualização 0.34.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.34.0.zip) · [Código-fonte 0.34.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.34.0.zip) · [Instalação e teste](docs/FASE-6.3A-0.34.0.md)

F10 → Barbearias / cortes e olhos: GPS de sete lojas, prévia de corte/textura e olhos freemode, cancelamento/restauração, confirmação de $150 pelo banco e último visual por perfil/modelo. Reaplicação manual sem cobrar de novo. E vanilla, tintura/barba/rosto/modelo/roupas preservados. 1208 testes aprovados e build sem erros/avisos; aparência precisa de teste no GTA. Saves/configurações e sistemas anteriores mantidos; cores/barba/editor facial e fases 8/9 continuam pendentes.

## Fase 6.2C — armazenamento privado do apartamento

[Atualização 0.33.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.33.0.zip) · [Código-fonte 0.33.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.33.0.zip) · [Instalação e teste](docs/FASE-6.2C-0.33.0.md)

Dentro do seu Eclipse: F10 → Moradia → Armazenamento do apartamento. Mesmo Inventory.json, depósito privado de 200kg e transferências de unidade/até cinco/pilha, sem parcial. Corrige rollback de falha de gravação também no stash/porta-malas antigo, mantendo F8. 1183 testes aprovados e build sem erros/avisos. Propriedade/roupas/itens/configs/F2/F11 preservados; menus/acesso precisam de teste no GTA. Demais diferenças e fases 8/9 mantidas.

## Fase 6.2B — guardarroupa e roupas

[Atualização 0.32.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.32.0.zip) · [Código-fonte 0.32.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.32.0.zip) · [Instalação e teste](docs/FASE-6.2B-0.32.0.md)

No seu Eclipse, suba ao guardarroupa e E, ou F10 → Moradia → Guardarroupa / roupas salvas. Biblioteca por perfil/modelo: salvar, vestir, substituir, renomear e excluir; editor de peças/texturas/acessórios com prévia reversível. Não muda ped/cabeça/cabelo nem cobra. 1153 testes aprovados e build sem erros/avisos. Saves/propriedade/configurações/F2/F11 preservados; roupas e cancelamento precisam de teste no GTA. Armazenamento/editor facial/hotéis e demais diferenças continuam; fases 8/9 mantidas.

## Fase 6.2A — moradia / Eclipse Towers

[Atualização 0.31.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.31.0.zip) · [Código-fonte 0.31.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.31.0.zip) · [Instalação e teste](docs/FASE-6.2A-0.31.0.md)

F10 → Moradia / Eclipse Towers → GPS; E na entrada. Compra única de $1425000 pelo banco RPCore e acesso ao apartamento com interior/piso verificados, saída pública e propriedade por perfil. Atualizar não compra imóvel nem cobra. 1125 testes aprovados e build sem erros/avisos. Saves/configurações/F2/F11 e PDM preservados; acesso/interior precisam de teste no GTA. Guardarroupa/storage/hotéis e demais diferenças continuam; fases 8/9 mantidas.

## Fase 6.1L — cofre administrativo e seguro PDM

[Atualização 0.30.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.30.0.zip) · [Código-fonte 0.30.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.30.0.zip) · [Instalação e teste](docs/FASE-6.1L-0.30.0.md)

F10 → Trabalhos → PDM → Cofre / seguro / ocorrencias. Cofre fechado e sem apólice por padrão: atualização não habilita perdas/cobranças. Abertura voluntária permite exposição ao se afastar; câmeras/guardas pagos reduzem risco. Seguro opcional tem prêmio inicial/renovação, restitui somente perda coberta e encerra sem caixa. 1099 testes aprovados e build sem erros/avisos. Saves/configurações/F2/F11 preservados; teste no GTA necessário. Sem missão física de roubo ou seguro de veículo pessoal; demais diferenças e fases 8/9 mantidas.

## Fase 6.1K — câmeras e monitoramento PDM

[Atualização 0.29.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.29.0.zip) · [Código-fonte 0.29.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.29.0.zip) · [Instalação e teste](docs/FASE-6.1K-0.29.0.md)

F10 → Trabalhos → PDM → Cameras / monitoramento. Compra única padrão $15000 do caixa, três equipamentos nas posições da referência e monitor ao vivo no escritório externo. Setas trocam canais; Backspace/Esc sai. Sem teleporte/invulnerabilidade, cobrança repetida ou remoção de props externos. 1063 testes aprovados e build sem erros/avisos. Saves/configurações/F2/F11 preservados; enquadramentos/renderização precisam de teste no GTA. Sem gravação, seguro/roubo ou PDM completa; fases 8/9 mantidas.

## Fase 6.1J — segurança presencial PDM

[Atualização 0.28.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.28.0.zip) · [Código-fonte 0.28.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.28.0.zip) · [Instalação e teste](docs/FASE-6.1J-0.28.0.md)

F10 → Trabalhos → PDM → Seguranca / postos presenciais. Dois guardas contratados/pagos no menu de equipe, postos da referência, defesa contra NPCs ambientes atacando jogador/equipe dentro de 20m e retorno a pé. Dívida desarma; procurado/missão suspende. Não gera ataques nem altera polícia/relações globais. Padrão $1300 contratação e $260 por 300s ativos por guarda. 1035 testes aprovados e build sem erros/avisos; combate/caminhos precisam de teste no GTA. Saves/configurações/F2/F11 preservados. Seguros/roubos/CCTV e outras diferenças permanecem; fases 8/9 mantidas.

## Fase 6.1I — equipe presencial e manutenção

[Atualização 0.27.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.27.0.zip) · [Código-fonte 0.27.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.27.0.zip) · [Instalação e teste](docs/FASE-6.1I-0.27.0.md)

F10 → Trabalhos → PDM → Equipe presencial / manutencao. Mecânico e zeladoria contratados caminham até os carros expostos para reparar/limpar, mantendo vaga e estoque. Reparo cobrado só ao concluir; limpeza sem taxa adicional, com salário devido. Sujeira progride no showroom e reduz novas cotações; limpeza recupera preço. Contrate/pague em Equipe / salarios / melhorias. 998 testes aprovados e build sem erros/avisos. Saves/configurações/F2/F11 preservados; NPCs, caminhos e animações precisam de teste no GTA. Limpeza de piso/lixo, seguranças e demais diferenças continuam pendentes; fases 8/9 mantidas.

## Fase 6.1H — showroom e atendimento

[Atualização 0.26.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.26.0.zip) · [Código-fonte 0.26.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.26.0.zip) · [Instalação e teste](docs/FASE-6.1H-0.26.0.md)

F10 → Trabalhos → PDM → Showroom / vagas / compradores. Dezenove posições da referência, estoque atribuído persistentemente, temporários/preços, recolhimento seletivo e atendimento ao comprador na entrada externa com E. Venda credita caixa e baixa carro/vaga uma vez. Sem criar estoque grátis, apagar ambiente ou forçar portas/interior; comece testando vagas Patio. 965 testes aprovados e build sem erros/avisos. Saves/configurações/F2/F11 e sistemas anteriores preservados; posições/interiores/cliente precisam de teste no GTA. Demais subentregas e fases 8/9 mantidas.

## Fase 6.1G — pedidos da locadora

[Atualização 0.25.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.25.0.zip) · [Código-fonte 0.25.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.25.0.zip) · [Instalação e roteiro](docs/FASE-6.1G-0.25.0.md)

F10 → Trabalhos → PDM → Pedidos da locadora / dois carros. Duas unidades prontas do mesmo modelo: carregar Packer/TRFlat, entregar no pátio externo do aeroporto, buzinar para o funcionário, aguardar aceite e voltar à PDM para receber. Dois Asea intactos rendem 21280 no caixa no padrão. Estoque pago, danos e checkpoints impedem duplicação de venda/pagamento. 934 testes aprovados e build sem erros/avisos; percurso precisa de teste no GTA. Configurações/saves/F2/F11 e sistemas anteriores preservados; demais subentregas e fases 8/9 mantidas.

## Fase 6.1F — inadimplência e retomada PDM

[Atualização 0.24.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.24.0.zip) · [Código-fonte 0.24.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.24.0.zip) · [Instalação e teste](docs/FASE-6.1F-0.24.0.md)

Novos financiamentos têm risco padrão de 20% por parcela; contratos da 0.23.0 continuam com risco zero. Financiamentos → contrato em atraso → Retomada: Packer/TRFlat, buscar o carro estacionado, E carrega vazio, voltar e descarregar. Retorna ao estoque com danos, encerra saldo a receber e preserva valores recebidos, sem bônus. Custo original volta ao estoque para não ser contado duas vezes na revenda. 898 testes aprovados e build sem erros/avisos; percurso físico precisa de teste no GTA. Guia inclui como testar com risco 100% em um contrato novo. Variantes dirigindo/confronto e demais subsistemas continuam em subentregas/refinamento; fases 8/9 mantidas.

## Fase 6.1E — venda financiada PDM

[Atualização 0.23.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.23.0.zip) · [Código-fonte 0.23.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.23.0.zip) · [Instalação e roteiro completo](docs/FASE-6.1E-0.23.0.md)

F10 → Trabalhos → PDM → Estoque → carro pronto → Entregar financiado. Entrega física ao cliente confirma entrada e contrato; parcelas seguem no caixa, com consulta em Financiamentos / parcelas. Asea padrão sem danos/divulgação: total 12320, entrada 3080, quatro parcelas de 2310 a cada dois minutos ativos. Venda à vista e saves anteriores preservados. 860 testes aprovados, build sem erros/avisos; precisa testar no GTA. Esta subfase cobre clientes adimplentes; inadimplência/retomada ficam para a próxima entrega PDM. Fases 8/9 mantidas.

## Fase 6.1D — coleta de valores PDM

[Atualização 0.22.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.22.0.zip) · [Código-fonte 0.22.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.22.0.zip) · [Instalação e roteiro de teste](docs/FASE-6.1D-0.22.0.md)

F10 → Trabalhos → PDM → Coleta de valores / carro-forte. Reserva parte do caixa, Stockade chega, segurança caminha à entrada, E entrega a maleta e saída/compensação transfere ao banco pessoal. Antes da entrega, cancelar devolve ao caixa; depois, recuperação conclui no banco uma vez. Sem bônus nem dinheiro novo. Caixa/estoque/pessoal/logística e saves anteriores preservados. 824 testes aprovados e build sem erros/avisos. Trânsito/NPC/maleta precisam de teste no GTA. Outros subsistemas PDM e fases 8/9 permanecem na sequência.

## Fase 6.1C — equipe, salários e melhorias PDM

[Atualização 0.21.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.21.0.zip) · [Código-fonte 0.21.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.21.0.zip) · [Instalação e teste](docs/FASE-6.1C-0.21.0.md)

F10 → Trabalhos → PDM → Equipe / salarios / melhorias. Mecânico e divulgação júnior/sênior/diretor; salários a cada cinco minutos ativos, pagamento com caixa empresarial, demissão com acerto proporcional/multa, oficina reduzindo reparos e divulgação bonificando vendas concluídas. Sem cobrança offline ou uso automático do banco pessoal. Caixa/estoque/coleta/exposição e saves anteriores preservados. 783 testes aprovados e build sem erros/avisos; funcionamento no GTA depende do roteiro de teste. Demais subsistemas PDM seguem em subentregas; fases 8/9 mantidas.

## Fase 6.1B — coleta PDM no porto

[Atualização 0.20.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.20.0.zip) · [Código-fonte 0.20.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.20.0.zip) · [Instalação e roteiro de teste](docs/FASE-6.1B-0.20.0.md)

PDM 0.19.0 aprovada pelo usuário; refinamento posterior mantido. Novos pedidos pagos aguardam no porto: Packer + TRFlat, coleta de um/dois carros, carga assistida com E, retorno e recebimento. Estoque antigo continua pronto. Uma unidade pronta pode ser exposta no pátio externo. Sem nova cobrança/pagamento por transportar estoque próprio; venda/caixa/cliente anteriores preservados. 745 testes aprovados e build sem erros/avisos. Percurso físico precisa do teste no GTA. Próximas subentregas PDM: equipe/infraestrutura e demais subsistemas; fases 8/9 mantidas.

## Fase 6.1A — núcleo da empresa PDM

[Atualização 0.19.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.19.0.zip) · [Código-fonte 0.19.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.19.0.zip) · [Instalação, alterações e teste](docs/FASE-6.1-0.19.0.md)

Arrancada confirmada funcionando pelo usuário. Nova empresa: F10 → Trabalhos → PDM / empresa e entregas → GPS. Adquira por 25000 do banco, aporte 10000, compre Asea por 8000 do caixa, entregue ao cliente e passe as chaves com E. Venda sem danos credita 11200 no caixa da empresa; saque depois no escritório. Estoque/danos/reparo/recuperação persistentes, caixa separado e carros pessoais preservados. 694 testes e build sem erros/avisos; ciclo físico precisa de teste no GTA. Logística/showroom, equipe e financiamento/aluguel seguem em subentregas PDM. Fases 8/9 mantidas.

## Histórico — fase 5.6: arrancada

[Atualização 0.18.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.18.0.zip) · [Código-fonte 0.18.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.18.0.zip) · [Instalação, alterações e teste](docs/FASE-5.6-0.18.0.md)

F10 → Trabalhos → Arrancada / Sandy Shores → GPS. Traga carro próprio, escolha treino/aposta, alinhe no verde e E inicia contagem. Segure o freio até JA; cruze antes do NPC. Pontos/banner da referência, IA real, queima, chegada interpolada, aposta reservada e resultado salvo antes do pagamento. Carro próprio preservado; streamer continua paralelo. 653 testes e build sem erros/avisos. Corrida/IA precisam de teste no GTA. Última atividade base da fase 5; depois segue fase 6. Fases 8 (interface/mecânicas) e 9 (todos os trabalhos) mantidas.

## Histórico — fase 5.5: streamer

[Atualização 0.17.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.17.0.zip) · [Código-fonte 0.17.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.17.0.zip) · [Instalação, alterações e teste](docs/FASE-5.5-0.17.0.md)

F2 → Twitch ou F10 → Trabalhos → Streamer. Canal/título, live local de 180 s em seis blocos de 30 s, audiência/chat contextual, seguidores/inscrições/bits/doações, banco e estatísticas no mesmo checkpoint. Usa TwitchOverlay/TwitchIcon originais. Pode transmitir enquanto trabalha; End continua reservado aos trabalhos. Menus/pausa não avançam o relógio. 614 testes, build sem erros/avisos; novo ciclo/app/painel precisam de teste no GTA. Preserva F2, saves e sistemas anteriores. Arrancada é a próxima atividade; fases 8 (interface/mecânicas) e 9 (todos os trabalhos) mantidas.

## Histórico — fase 5.4: tráfico / vendas de rua

[Atualização 0.16.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.16.0.zip) · [Código-fonte 0.16.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.16.0.zip) · [Instalação, alterações e teste](docs/FASE-5.4-0.16.0.md)

F10 → Trabalhos → Trafico / vendas de rua. GPS do contato externo; compre cinco pacotes por 200 do banco. Inicie em calçada pública a pé/desarmado e aguarde comprador laranja. E negocia preço normal/premium; fique junto por três segundos para confirmar. Três vendas por turno, estoque/reputação/pagamento no mesmo checkpoint, recusa e risco de uma estrela. Banner original DrugDealingBanner. 567 testes e build sem erros/avisos; ciclo 0.16.0 testado e confirmado funcionando pelo usuário no GTA. Saves/F2 e sistemas anteriores preservados. Próximas atividades: streamer e arrancada; refinamentos dos trabalhos após as fases principais.

## Fases de refinamento definidas pelo usuário

Após concluir as fases principais, a **fase 8** refina toda a interface e suas mecânicas: fome/sede e demais indicadores do HUD, F11 e todos os menus existentes. O problema relatado de fome/sede é no funcionamento da mecânica, não somente na aparência dos ícones. A **fase 9** refina todos os trabalhos, cujos ciclos atuais ainda têm simplificações. Ambas exigem varredura aprofundada da PSRP.dll e comparação com o RPCore antes de implementar os ajustes, preservando saves, recursos e atalhos. [Escopo e critérios](docs/REFINAMENTOS-FASES-8-9.md).

## Histórico — fase 5.3: crafting e feira

[Atualização 0.15.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.15.0.zip) · [Código-fonte 0.15.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.15.0.zip) · [Instalação, alterações e teste](docs/FASE-5.3-0.15.0.md)

Pesca confirmada funcionando pelo usuário. Nova bancada/feira: F10 → Trabalhos → Bancada / crafting e feira → GPS. Leve materiais das bolsas de caça/pesca sem vendê-los, guarde a arma e fabrique. Nove receitas, sete produtos, lotes de um/cinco, tempo, estoque e venda bancária com retirada/criação no mesmo checkpoint. Produtos são mercadorias no estoque da bancada; Inventory.json continua igual. 518 testes e build sem erros/avisos. Novo ciclo precisa de teste real no GTA. Tráfico, streamer e arrancada seguem na fase 5; refinamentos dos trabalhos ficam para depois das fases principais.

## Histórico — fase 5.2: pesca

[Atualização 0.14.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.14.0.zip) · [Código-fonte 0.14.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.14.0.zip) · [Alterações, instalação e teste](docs/FASE-5.2-0.14.0.md)

F10 → Trabalhos → Pesca. Na banca, equipamento 350 + cinco iscas 50 do banco. Inicie, vá ao pier, guarde a arma e E olhando para água. Espere a fisgada: E, depois três puxadas E espaçadas. Complete três peixes e venda a bolsa na banca. Peixes/iscas/equipamento persistem por perfil, com checkpoint bancário e recibos. Três pontos configuráveis. Build sem erros/avisos e 477 testes aprovados; falta teste real de água/posições/vara e ciclo no GTA. F2, caça, saves e trabalhos anteriores preservados. Crafting/comércio segue na fase 5; refinamentos dos trabalhos ficam para depois das fases principais.

## Histórico — fase 5.1: caça

[Atualização 0.13.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.13.0.zip) · [Código-fonte 0.13.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.13.0.zip) · [Como jogar, alterações e teste](docs/FASE-5.1-0.13.0.md)

**F10 → Trabalhos → Caca → GPS: acougue / equipamento.** Compre licença/kit e iscas, inicie a expedição, siga à floresta verde, E coloca isca, abata o animal laranja e E coleta. Bolsa de caça persistente com carne/peles/troféus; venda no açougue credita o banco.

Piloto e teleporte foram aprovados pelo usuário. Os oito trabalhos anteriores, F2, saves/configurações e recursos visuais ficam preservados. Build sem erros/avisos e 430 testes aprovados; a caça ainda precisa do teste no GTA. Pesca/crafting e demais atividades da fase 5 ficam nas próximas subentregas. Refinamento dos trabalhos aprovados fica para depois das fases principais, conforme combinado.

## 0.12.2 — teleporte para agilizar testes

[Atualização 0.12.2 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.12.2.zip) · [Código-fonte 0.12.2](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.12.2.zip) · [Como usar e testar](docs/TELEPORTE-0.12.2.md)

**Marque o ponto no mapa → F11 → Teleportar para o waypoint.** A opção fica no topo do menu. Funciona a pé e como motorista de carro/moto/ônibus parado; carrega o chão e procura posição livre antes de mover. Falha/cancelamento restauram o estado anterior. Não muda qualificação/dinheiro/procurado.

Inclui a base pública de piloto em Grapeseed. F2, saves, configurações e trabalhos anteriores preservados. Build sem erros/avisos e 396 testes de regressão aprovados; o usuário confirmou teleporte e piloto funcionando no GTA. Não precisa reinstalar recursos visuais.

## Correção 0.12.1 — base pública de aviação

[Atualização 0.12.1 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.12.1.zip) · [Código-fonte 0.12.1](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.12.1.zip) · [Como realizar o trabalho e testar](docs/CORRECAO-0.12.1.md)

**F10 → Trabalhos → Piloto → GPS: base de aviacao.** A base padrão agora é McKenzie Field/Grapeseed, evitando a área restrita do LSIA. Qualificação e avião continuam até Sandy Shores; helicóptero mantém cinco destinos e retorna à nova base. Prazo executivo padrão Arcadius: 240 segundos.

Configuração LSIA antiga padrão é ajustada somente em memória; Pilot.json, licença, saves, F2 e personalizações preservados. Sem compra de hangar ou limpeza de estrelas. Build sem erros/avisos e 396 testes aprovados; novo spawn/acesso e ciclo completo ainda precisam do teste no GTA. Não precisa reinstalar recursos visuais.

## Histórico — fase 4.4

[Atualização 0.12.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.12.0.zip) · [Código-fonte 0.12.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.12.0.zip) · [Instruções, alterações e teste](docs/FASE-4.4-0.12.0.md)

**F10 → Trabalhos → Piloto → GPS: balcao LSIA.** Comece pelo Voo de qualificacao até Sandy Shores. Licença salva por perfil libera transporte de avião e helicóptero, com passageiros, desembarque físico, pagamento bancário e retorno para bônus. X/tecla de motor ou menu religa a aeronave após a entrega. Helicóptero atende cinco destinos, com prazo no voo executivo Arcadius.

Transporte prisional da 0.11.0 aprovado pelo usuário. Sete trabalhos anteriores, F2, saves e configurações preservados. Build sem erros/avisos e 385 testes aprovados. Qualificação, portas, pousos e ciclo completo ainda precisam do teste no GTA. Não precisa reinstalar recursos visuais. Dirigível/publicidade e desvio/suborno/emboscada prisional continuam pendentes.

## Fase 4.3 — transporte prisional regular

[Atualização 0.11.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.11.0.zip) · [Código-fonte 0.11.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.11.0.zip) · [Instruções, arquivos alterados e teste](docs/FASE-4.3-0.11.0.md)

**F10 → Trabalhos → Transporte prisional → GPS: delegacia Mission Row.** Contrate a pé, retire o PBus, confira quatro detentos parado com E, leve a Bolingbroke, aguarde o desembarque físico e retorne vazio à delegacia. Pagamento no banco por custódia e bônus ao devolver o ônibus.

O usuário confirmou a correção LS GAS 0.10.1 funcionando. Os seis contratos anteriores permanecem; telefone F2/saves/configurações preservados. Build sem erros/avisos e 340 testes aprovados. IA/portas do PBus e ciclo completo precisam do teste no GTA. Suborno/emboscada da referência ficam separados e pendentes; piloto vem na próxima entrega de empregos. Não precisa reinstalar recursos visuais.

## Correção 0.10.1 — pátio LS GAS

[Atualização 0.10.1 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.10.1.zip) · [Código-fonte 0.10.1](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.10.1.zip) · [Alterações e teste](docs/CORRECAO-0.10.1.md)

Corrigido o bloqueio amplo de “Patio LS GAS ocupado”: seleção de até 35 pares de vagas próximas com dimensões reais dos modelos, chão e colisão do cenário. O contrato procura alternativa se a posição original estiver ocupada. Saves, celular F2 e FuelDelivery.json preservados; sem reinstalar recursos visuais. Build zero erros/avisos e 309 testes aprovados. O usuário confirmou a 0.10.1 funcionando no GTA após a correção.

## Fase 4.2 — transporte de combustível

[Atualização 0.10.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.10.0.zip) · [Código-fonte 0.10.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.10.0.zip) · [Como jogar, arquivos alterados e teste](docs/FASE-4.2-0.10.0.md)

**F10 → Trabalhos → Transporte de combustivel → GPS: deposito LS GAS.** Contrate no porto, retire o Hauler, engate o tanque, descarregue no posto parado com motor desligado e retorne com o conjunto para o bônus. Pagamento bancário na descarga e no retorno; recuperação de engate e proteção das duas partes.

Reposição de lojas aprovada pelo usuário: cinco entregas e pagamento corretos. Os cinco trabalhos anteriores continuam disponíveis. Celular F2 e saves/configurações preservados. Build sem erros/avisos e 295 testes aprovados; esta entrega ainda precisa do teste de engate, descarga e retorno no GTA. Não precisa reinstalar OIV/YTD/DLC. Transporte prisional e piloto ficam para próximas entregas.

## Histórico — fase 4.1: reposição de lojas

[Atualização 0.9.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.9.0.zip) · [Código-fonte 0.9.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.9.0.zip) · [Como trabalhar, arquivos alterados e teste](docs/FASE-4.1-0.9.0.md)

**F10 → Trabalhos → Reposicao → GPS.** Retire o Mule2 em Davis, carregue na base, entregue cinco caixas nas lojas e devolva o caminhão para o bônus. Crédito bancário por descarga, prioridade de E no destino e progresso preservado. Usa os recursos visuais já instalados.

**Celular F2**, respeitando o INI existente. Garagem usa O quando a tecla anterior conflita com o telefone, sem regravar sua configuração. Pizza/Post OP/Uber/valet foram testados e aprovados pelo usuário no GTA. Esta fase tem build sem erros/avisos e 256 testes aprovados; a reposição foi confirmada pelo usuário no GTA. Combustível foi acrescentado na fase 4.2; prisão e piloto ficam para entregas seguintes.

## Histórico — fase 3: Uber e valet

[Atualização 0.8.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.8.0.zip) · [Código-fonte 0.8.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.8.0.zip) · [Como jogar, alterações e teste desta fase](docs/FASE-3-0.8.0.md)

**Uber: pare um carro vazio como motorista → F1 → Uber → Entrar em serviço.** Aceite a solicitação, busque o passageiro, E/buzina para embarcar e E no destino parado para desembarcar. Pagamento no banco após o NPC sair; tarifa, gorjeta e avaliação persistentes. O celular abre no carro parado e fecha ao dirigir.

**Valet: F10 → Trabalhos → Valet → GPS.** Contrate no hotel a pé, receba o carro do cliente, estacione/tranque, retorne ao hotel, solicite a devolução e traga o mesmo carro ao dono. Pagamento após o cliente voltar ao banco do motorista. Três atendimentos por turno por padrão; End cancela. Pizza e Post OP continuam disponíveis com seus ciclos validados.

Build zero erros/avisos e 226 testes aprovados. O usuário confirmou os turnos completos e pagamentos de pizza/Post OP da 0.7.0 no GTA. O usuário também confirmou Uber e valet funcionando na 0.8.0; a próxima entrega é a fase 4.1. Nenhuma reinstalação de OIV/YTD/DLC necessária. [Roteiro de teste e limites](docs/FASE-3-0.8.0.md#teste-no-gta-desta-entrega).

## Histórico — fase 2: Post OP e contrato de pizza

[Atualização 0.7.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.7.0.zip) · [Código-fonte 0.7.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.7.0.zip) · [Como jogar, alterações e teste desta fase](docs/FASE-2-0.7.0.md)

**F10 → Trabalhos / contratos → Post OP → GPS.** Vá ao marcador a pé, E → Iniciar contrato, entre na van azul e siga o GPS. No endereço, pare, saia, retire a caixa com E na traseira e entregue a pé com E na porta. Cada entrega paga no banco. Complete cinco e volte ao depósito para o bônus de retorno. Segure End por um segundo ou use o menu do contrato para cancelar.

A pizzaria agora também oferece contrato com scooter, pizza na mão, entrega na porta e retorno. **F6 mantém o modo rápido anterior**, inclusive com veículo próprio. Contratos também estão em F11/Game Settings, GPS e F1/Settings/Contacts. Um turno por vez; confirmação persistente de pagamento, cancelamento e preservação dos saves. O catálogo de jobs vem na DLL e pode ser personalizado em Jobs/PostOp.json. Banners usam os YTDs já instalados.

Build zero erros/avisos e 166 testes aprovados. A 0.6.0 foi confirmada pelo usuário no GTA; a 0.7.0 precisa do teste de van/endereços/carga/animações. [Roteiro completo de teste](docs/FASE-2-0.7.0.md#teste-no-gta-desta-entrega). Uber e valet ficam para a fase 3.

## Fase 1 — conveniências e mapa

[Atualização 0.6.0 — DLL pronta](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.6.0.zip) · [Código-fonte 0.6.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.6.0.zip) · [Comparação RPCore × PSRP e próximas fases](docs/FASES-0.6.0.md)

Pontos de compra corrigidos pelas coordenadas da PSRP.dll, incluindo LTD Davis, e catálogo ampliado para 20 conveniências. Configuração antiga padrão é corrigida em memória, sem apagar Shops.json ou saves. Posições personalizadas são preservadas. Ícones com nomes/cor/escala por categoria, depósito agora no mapa, pizzaria com ícone e GPS e entrega ativa com pacote amarelo. F6 continua iniciando/encerrando o turno. OIV e DLC já instalados continuam válidos.

Build sem erros/avisos e 124 testes aprovados. A 0.5.1 foi confirmada funcionando no GTA pelo usuário; a 0.6.0 ainda precisa do teste dos novos pontos no jogo. [Teste desta fase e sequência dos jobs](docs/FASES-0.6.0.md#teste-solicitado-agora). Post OP, Uber e demais empregos continuam nas próximas fases; o comparativo distingue a lógica existente dos menus e classes vazias.

## YTD instalados — correção 0.5.1

[Atualização 0.5.1](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.5.1.zip) · [Código-fonte 0.5.1](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.5.1.zip) · [Auditoria da PSRP.dll e mapa dos recursos](docs/AUDITORIA-YTD-0.5.1.md)

O destino confirmado pelo OIV/print é mods/update/update.rpf/x64/data/cdimages/scaleform_generic.rpf. O RPCore usa os dicionários/imagens já montados pelo GTA; não abre esse RPF pelo caminho do Windows. Nomes e vínculos agora vêm também embutidos na DLL: faltar um JSON auxiliar não bloqueia mais as imagens padrão. Overrides locais continuam preservados. O banner F11 alterna as três imagens originais a cada cinco segundos, com altura 128 da referência.

F10 > Visual assets: diagnose agora verifica o carregamento e a resolução de cada textura, grava scripts/RPCore/VisualDiagnostics.json e um resumo no log. O relatório precisa ser gerado dentro do GTA. Dimensões iguais não provam renderização idêntica. Não precisa reinstalar os YTDs já instalados para esta atualização. Build sem erros/avisos e 112 testes aprovados.

## Interface 0.5.0 — referências do Downloads.zip

[DLL pronta 0.5.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.5.0.zip) · [Código-fonte 0.5.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.5.0.zip)

**Velocímetro no centro inferior**, seis indicadores no canto superior direito com dinheiro/banco alinhados, **inventário I em grade 5×5**, **celular F1 com wallpaper do carro vermelho/relógio/ícones do print**, radial J com tamanho corrigido e F11 com posição/fonte do menu de referência. F2 mantém garagens e F10 mantém banco/perfis/diagnóstico. O pacote visual 0.3.0 continua válido.

[Instalação, funções disponíveis, limites e testes da 0.5.0](docs/INTERFACE-0.5.0.md). A composição foi corrigida no código e validada nos dados; a renderização final precisa do teste no GTA. Notas/calculadora/pesquisa funcionam; Uber, chamadas de voz e SMS continuam pendentes. O inventário mostra durabilidade n/a; editor de objetos e crafting continuam pendentes. Não foi transplantada toda a lógica da PSRP.dll.

Configurações anteriores são preservadas. Os valores padrão antigos de NeedsTop (60/12) são reconhecidos em memória como o novo topo 20. As opções novas de Interface.ini usam defaults compatíveis mesmo se o INI antigo já existir. A unidade do desenho é mph por padrão, alterável em F11 > Vehicle Settings. A lista de inventário anterior permanece disponível no F11 > UI Settings.

## Histórico — interface 0.4.0

[Baixar atualização 0.4.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-update-0.4.0.zip) · [Código-fonte 0.4.0](https://github.com/weslleyWC/RPCore/raw/refs/heads/rpcore-entrega/downloads/RPCore-source-0.4.0.zip)

**J** abre o radial com oito setores originais do OIV, Q/E seleciona e Enter abre. **F11** abre Settings Menu com as oito categorias dos prints e o banner do carro preto. **F2** abre garagens; F10 continua com perfis/banco/diagnóstico. O HUD passa a seis indicadores reais e dinheiro compacto; inventário pessoal tem cabeçalho azul.

O usuário confirmou que a 0.3.0 funciona; esta nova composição ainda precisa do teste visual no jogo. Veja [ações implementadas, limites e testes da 0.4.0](docs/INTERFACE-0.4.0.md). Map Builder nesta versão edita pontos pessoais com GPS; não coloca objetos. A PSRP.dll foi usada como referência de interface e não deve ser instalada junto do RPCore. Não foram copiadas todas as suas funcionalidades.

O pacote visual 0.3.0 continua válido e não precisa ser reinstalado se já estiver funcionando. O instalador da atualização preserva saves/configurações e acrescenta Interface.ini quando ausente.


Projeto C# .NET Framework 4.8 para **ScriptHookVDotNet Enhanced 1.1.0.6** e **LemonUI SHVDN3 2.2**, as versões informadas pelo desenvolvedor. Implementação própria sobre os fontes enviados, mantendo `ISystem`, LemonUI e os formatos básicos de inventário, necessidades e pizza.

A primeira entrega (commit 6819140) foi testada no GTA pelo usuário e ele informou que funcionou. As adições 0.2.0 e a integração visual 0.3.0 foram compiladas/testadas nos dados e aguardam seus testes de gameplay.

## Histórico — recursos visuais da 0.3.0

A DLL agora usa os recursos **PSRP.oiv** e **psrp_props.zip** enviados pelo usuário: HUD de anéis/ícones, painel de veículo, banners, ícones de itens, apresentação de props e telefone F1 com atalhos para os sistemas existentes. Instalar somente a DLL não registra texturas ou DLC no GTA.

Baixe também **RPCore-visual-assets-0.3.0.zip**. Se já tiver esses mesmos recursos instalados, use F10 → Visual assets: diagnose antes de reinstalar. Instruções completas e checklist: [VISUAIS-0.3.0.md](docs/VISUAIS-0.3.0.md). O helper prepara o DLC sem editar update.rpf; a instalação do OIV e a entrada dlclist são feitas pelo OpenIV, preservando os outros mods.

`Visuals.ini` controla a aparência, escala, props e tecla do telefone. Sem o OIV/DLC, o RPCore conserva a interface básica e as funções existentes. Os originais estão em visual-assets no repositório e no pacote visual separado; o ZIP de fontes não duplica esses binários. Os arquivos de arte não incluem toda a lógica/layout original PSRP; a composição foi implementada para o RPCore.

## Instalar no GTA

1. Feche o GTA. Extraia **RPCore-update.zip** em uma pasta temporária, fora de `scripts`.
2. Com ScriptHookV, SHVDN Enhanced e LemonUI já instalados, execute no PowerShell:

   ```powershell
   .\Install-RPCore.ps1 -GtaPath 'D:\Games\Grand Theft Auto V'
   ```

3. O instalador salva a DLL anterior e toda a pasta `scripts/RPCore` em `RPCore-backups`, fora da pasta de scripts. Substitui somente `RPCore.dll`; copia configurações e recursos ausentes sem sobrescrever os existentes. Não substitui SHVDN, LemonUI, arquivos do jogo ou saves existentes. Newtonsoft.Json é instalado apenas se estiver ausente; uma versão incompatível bloqueia a instalação antes das alterações.
4. Inicie o modo história. F10 abre o menu principal. Não mantenha uma cópia renomeada da DLL antiga dentro de `scripts`, pois o SHVDN também a executaria.

Configurações antigas são preservadas: se `Settings.ini` já existir, acrescente as novas opções a partir de `payload/scripts/RPCore/Settings.ini` quando necessário. `Keys.ini`, seção `SETTINGS`, tem prioridade para `Inventory Key` e `Seatbelt Key`. Por isso o cinto usa **Z** no pacote fornecido; numa instalação antiga com outra tecla em `Keys.ini`, essa tecla continua valendo.

## Controles padrão

| Tecla | Ação |
|---|---|
| F1 | Celular: setas/Enter para aplicativos, Backspace para voltar, F1 para fechar (a pé) |
| I | Inventário em grade: setas/Enter; C para reorganizar; I/Backspace fecha |
| E | Abrir uma loja quando estiver próximo ao balcão e a pé |
| F6 | Iniciar/cancelar turno de pizza, com 5 entregas por padrão |
| F7 | Conquistas e progresso |
| Z | Colocar/retirar cinto em veículo compatível |
| F8 | Armazenamento, a pé junto ao porta-malas destrancado ou ao baú configurado |
| F5 | Bolsa simulada: depósito, saque, compra/venda e lista de acompanhamento |
| F4 | Gangues: GPS para HQ, afiliação presencial e saída da gangue |
| F3 | Rádio: selecionar/parar uma transmissão |
| F9 | Registrar coordenadas no log |
| F10 | Salvar, banco, configuração, importar e selecionar perfis |
| X | Ligar/desligar motor como motorista |
| C | Ativar/cancelar cruise acima de 29 km/h; freio/acelerador também cancelam |
| B / N | Seta esquerda / direita |
| H | Pisca-alerta |
| K | Menu do veículo: motor, cruise, setas, capô e porta-malas |
| G | Iniciar/parar abastecimento no posto, parado e com motor desligado |
| F2 | Garagens: GPS, compra, guardar/retirar e serviços |
| J | Radial: Q/E, Enter; J/Esc fecha |
| F11 | Settings Menu: oito categorias de configuração |
| F12 | Concessionária: GPS e compra junto ao PDM |

Altere as teclas em `Settings.ini` se conflitam com trainers ou outros mods. Use F10 → Reload configuration para reler opções e teclas. Para alterar quais sistemas ficam habilitados, reinicie o script após editar `Features`. Os novos controles, combustível e garagem usam `Vehicles.ini`; as teclas de motor, cruise e setas de `Keys.ini` têm prioridade se estiverem presentes. `Car Controller Key = N` do PSRP continua sem efeito; K abre o menu de veículo, evitando a colisão com a seta direita N.

## Dados e compatibilidade

- `Items.json` e `Shops.json` mantêm os formatos originais. `Inventory/*.json` permite listas adicionais de itens; o último arquivo em ordem alfabética pode substituir um ID do catálogo base. IDs desconhecidos nos saves são preservados.
- `SaveData/<perfil>` mantém necessidades, inventário, pizza, conquistas, carteira e afiliação. `Inventory.json` também contém os compartimentos de armazenamento, para transferir itens e salvar ambos os lados em um arquivo atômico. Há `.bak` da versão anterior. Um JSON que falhou na leitura fica protegido contra gravação automática naquela sessão.
- Saves antigos do RPCore diretamente em `SaveData` são copiados uma única vez para o primeiro perfil detectado. `FlatMigration.json` registra o destino; os originais permanecem intactos. Faça a primeira inicialização com o protagonista dono desses saves.
- Michael, Franklin e Trevor têm perfis separados. Peds freemode usam o perfil selecionado no F10, persistido em `ActiveProfile.json`. Modelos desconhecidos/transições mantêm o último perfil. O RPCore não troca aparência ou modelo automaticamente.
- Arquivos originais do PSRP ficam em `assets/RPCore` nos fontes e em `scripts/RPCore/Legacy` no pacote, com áudio e gangues nos diretórios correspondentes. F10 importa os três jogadores enviados ou saves encontrados em `scripts/PSRP/SaveData/Players`. A importação nunca sobrescreve um perfil existente: cria `PSRP_<nome>` e retém uma cópia byte a byte do JSON completo, com todos os 151 campos.
- Importação funcional: fome, hidratação, dinheiro, banco, gangue/reputação, carteira de ações e conquistas já desbloqueadas. Campos de aparência, veículos PSRP, negócios, dívidas e territórios são preservados na cópia original; não são aplicados por esta versão. O novo cadastro de veículos RPCore é separado dos veículos PSRP antigos. A importação não entrega dinheiro de um save MP para um protagonista.
- `Wallet.json` é o checkpoint principal de dinheiro MP, banco e bolsa. Os protagonistas usam o dinheiro nativo do GTA; o salvamento desse dinheiro depende também do save do próprio jogo. A carteira em `StockMarket/<perfil>.portfolio.json` é exportada como cópia compatível; a bolsa RPCore é uma simulação local com três ativos e variação por minuto, sem integração com BAWSAQ/LCN.
- Porta-malas são identificados por modelo + placa dentro do perfil. Duas cópias do mesmo modelo com a mesma placa compartilham o compartimento; use placas distintas. O baú padrão está em `(-14.1, -1440.2, 31.1)`, editável em `Storage/Locations.json`. Limites: 100 kg no porta-malas e 200 kg no baú padrão.
- Gangues leem os três registros de `Gangs/CustomGangs.json`: nome, descrição, HQ e cor. Entre a pé no HQ pelo menu; cada entrega de pizza concede reputação até 100. Esta versão não cria NPCs armados, não muda relacionamentos globais nem simula guerras territoriais.
- Rádio usa Windows Media Player COM e conexão de rede no computador do GTA. Instale/habilite esse componente do Windows se necessário e forneça URLs diretas de áudio MP3 em `CustomRadioStream.xml`. O endereço fornecido no PSRP aparenta ser uma página de rádio, não um fluxo MP3 garantido; falhas de conexão são exibidas e registradas. Desligue o rádio nativo manualmente para evitar sobreposição. Reprodução real exige teste no Windows.
- Áudios de cinto, compra, entrega e conquista usam os WAVs PCM enviados, sem conversão dos originais. Os demais WAVs estão preservados. Os dois `RivalDealer*.wav` são IEEE float: não são enviados ao `SoundPlayer`, que espera PCM.
- A versão 0.2.0 acrescenta motor/cruise/setas, combustível e garagem próprios, com opções independentes em `Vehicles.ini`. Se outro mod já controla combustível, use `[Features] Fuel = False` e reinicie o script. O RPCore não reescreve handling, limite nativo de velocidade, polícia, armas, mapas ou configurações de outros mods. As opções PSRP de sistemas ainda ausentes permanecem sem efeito automático.

## Veículos, postos, garagem e concessionária (0.2.0)

- **Motor e setas:** ações somente como motorista de carro/moto/quadriciclo. O menu K abre capô e porta-malas com o carro parado. Setas e controle de auto-start alterados pelo RPCore são restaurados/liberados ao sair, trocar de veículo ou recarregar.
- **Cruise:** C registra a velocidade atual acima de 29 km/h e fornece aceleração para mantê-la aproximadamente. Frear, acelerar manualmente, puxar freio de mão, colidir, sair do chão, parar o motor ou abrir um menu cancela. Não é piloto de direção nem freio automático de descida e não modifica handling/max-speed.
- **Combustível:** 26 postos com blips, listados em `Fuel/Stations.json`. Pare na área, desligue o motor com X e pressione G; G novamente interrompe. Também é possível abastecer a pé a até 5 m do carro. O preço é por litro, arredondando a cobrança da operação para dólares inteiros; saldo e espaço no tanque limitam a quantidade. O consumo padrão é 8 L/100 km mais 0,8 L/h em marcha lenta. Tanque vazio desliga o motor, mantendo a possibilidade de reabastecer. Pausa, cutscene e missões do GTA suspendem o sistema. O tanque inicial usa o valor nativo do veículo, não recebe combustível gratuito ao recarregar. HUD mostra litros e distância rodada pelo RPCore.
- **Exclusões:** bicicletas, aeronaves, barcos, trens, elétricos e modelos de `SaveData/ExcludedGasList.json` (ou o original em `Legacy/SaveData`) não recebem consumo/intervenção RPCore. Não há recarga elétrica nesta versão. Combustível é identificado por modelo + placa e fica em `Wallet.json` → `Transport.Tanks` por perfil.
- **Garagens:** F11 mostra GPS para três localizações: Strawberry ($10.000/3 veículos), Burton ($25.000/6), Sandy Shores ($8.000/2), editáveis em `Garages/Locations.json`. Compre perto do marcador; vagas incluem veículos associados que estão fora. Guarde parado, como motorista, sem passageiros/reboque. O jogo recebe uma tarefa de saída; mantenha-se perto por até 15 segundos. O snapshot é salvo antes de remover a entidade; uma marca pendente permite recuperar uma interrupção sem criar automaticamente outra cópia.
- **Retirada/recuperação:** usa a saída configurada e recusa saída bloqueada. Primeiro procura modelo/placa no mundo; se o veículo existir, marca GPS em vez de duplicar. Se não existir, recria o último snapshot salvo. Modelo de addon ausente não apaga o cadastro. Não há seguro nem recuperação de estado posterior ao último save. Placas repetidas não são suportadas para veículos diferentes do mesmo modelo.
- **Personalização/danos:** preserva placa/estilo, cores normais e RGB, cores extras, roda, até 50 slots de mods disponíveis, variações, toggles, extras, neon, fumaça, película, livery, saúde de motor/carroceria/tanque, sujeira, combustível, pneus furados, vidros e portas quebrados. A forma exata de amassados, marcas de balas, ocupantes e flags/scripts de outros mods não é serializada. Guarde veículos parados sem ocupantes; veículos de missão são recusados por padrão. `AllowMissionVehicles = True` é uma opção explícita para veículos de seus mods/trainer; não muda a proibição durante missões ativas.
- **Serviços:** na própria garagem, como motorista parado, reparar custa $500 e lavar $25. Reparar preserva o combustível. Não há reparo automático gratuito nem intervenção em veículos de missão protegidos.
- **Concessionária:** F12 define GPS para o PDM; perto da entrada, permite comprar Faggio, Asea, Blista, Dominator e Sultan. `Dealership/Vehicles.json` aceita outros modelos/preços. É necessário ter vaga em uma garagem própria; a compra registra o carro, gera uma placa RP única e entrega na saída. Modelo ausente, saldo insuficiente ou saída bloqueada não devem cobrar. Isso implementa compra pessoal de veículos; a empresa PDM possui núcleo comercial e coleta nas fases 6.1A/B; funcionários e demais subsistemas empresariais continuam em próximas subentregas.
- **Persistência:** garagem, veículos e combustível usam `Wallet.json` → `Transport`. Para peds MP, o pagamento e esses dados usam o mesmo checkpoint atômico da carteira. Nos protagonistas, o dinheiro nativo também depende do save GTA, como antes. A atualização lê saves 0.1 sem conversão destrutiva. As conquistas PSRP de primeiro carro, primeira garagem e primeiro abastecimento agora têm gatilhos.

O checklist específico da nova DLL está em [TESTES-0.2.0.md](docs/TESTES-0.2.0.md). Configurações antigas são mantidas; as novas ficam em arquivos separados copiados pelo instalador.

Veja [o relatório de alterações](docs/ALTERACOES.md) e [os testes no GTA](docs/TESTES-GTA.md). A compilação e os testes de dados executam no Linux; a execução do jogo e do áudio COM exige Windows/GTA.

## Compilar e validar

Linux/cloud, dentro deste checkout isolado (sem criar worktree):

```bash
bash scripts/setup.sh
```

O script instala SDK .NET 8.0.419 em `/workspace/.tools`, baixa as referências oficiais verificando SHA-256, restaura com lockfiles, compila `net48` usando reference assemblies e executa os testes de dados em `net8.0`. Não inicia o GTA nem instala DLLs de runtime no jogo.

Windows com SDK .NET e as DLLs instaladas no GTA:

```powershell
dotnet build RPCore.csproj -c Release -p:GTAV='D:\Games\Grand Theft Auto V'
dotnet test tests/RPCore.Tests.csproj -c Release
python scripts/package.py
```

Se `.deps` existir, ela tem prioridade. Use `-p:ReferenceDir='D:\referencias'` para indicar outra pasta com as duas DLLs de referência. `python scripts/package.py` gera `dist/RPCore-update.zip` e `dist/RPCore-source.zip` após a compilação; com os originais em visual-assets também gera `dist/RPCore-visual-assets.zip`. Nenhum teste externo afirma ter executado o GTA.
