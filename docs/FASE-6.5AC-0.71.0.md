# Fase6.5AC — estreia Oriental Theatre — 0.71.0

## Instalação

Baixe RPCore-update-0.71.0.zip da branch rpcore-entrega, extraia e execute Install-RPCore.ps1 **da pasta extraída**, com -GtaPath apontando à pasta real que contém GTA5.exe. GTA V Legacy, SHVDN Enhanced1.1.0.6 e LemonUI2.2 SHVDN3. Preserva saves/configs, celularF2, teleportF11 e recursos .oiv/.rpf/YTD/props instalados. PSRP.dll foi referência estática, não dependência para instalar junto.

## Teste no GTA

1. Sua gangue precisa possuir o **território9**. F4 → Comercio dos territorios → área9 → **GPS: equipamentos Oriental Theatre**.
2. A pé a até8m da caixa inicial(308.4233,197.2859,103.1828), seguro e sem outra atividade/pendência, selecione **Montar estreia /11 pecas**. Gratuito.
3. Na caixa, **E a até2,3m** inicia coleta por1,5s ativos. Permaneça perto e a pé. Caixa própria fica presa à mão direita com animação de carregar.
4. Vá **a pé** até o ponto indicado. **E a até2,3m** instala por1,5s ativos. Fique perto até confirmar. A peça aparece, a caixa carregada sai da mão e a instalação fica salva.
5. Volte à caixa e repita para cada uma das **11 peças**, na ordem da PSRP: duas luzes, três cercas de entrada, cinco cercas e a última peça chamada Red Carpet.
6. Depois da última instalação, **E ali ou na caixa inicial** confirma a estreia. Só essa confirmação libera processamento2X na área9 por60minutos reais e solicita o cenário redCarpet por10minutos de jogo.
7. Confira o tapete vermelho e a linha Estreia /bonus2X no F4. Bônus não liga automático sozinho nem cria receita/pagamento pessoal. Com receita e automático ativos, lote padrão até$500 passa de60s para aproximadamente30s ativos;85% líquido ao caixa/15% taxa continuam. Nove atividades usam máximo2X, sem multiplicação e sem renda offline.

## Suspender, retomar e cancelar

**END suspende**, mantendo coleta atual e instalações concluídas. F4 permite retomada/GPS/cancelamento.

- Parcial: retome **a pé a até115m da caixa inicial**, com posse/segurança. Recria visualmente as peças já instaladas e a caixa na mão se a coleta foi salva; continua da instalação atual, sem refazer anteriores.
- Pronto11/11: retome a pé a até2,3m da caixa inicial e confirmeE. Não recria caixa/peças nem repete transporte.
- Cancelar: gratuito, pode sem posse/filiação em segurança. Remove pendência sem prêmio parcial e conserva bônus anterior. Receita/estoques pessoais e outros contratos ficam intactos.
- Pausa/menu, afastamento durante a troca ou sair do alcance interrompem somente a troca ainda não confirmada. TenteE novamente. Não geram comprovante parcial.
- Entrar em veículo carregando suspende e mantém Carrying salvo; saia e retome perto do teatro. Atividade de transporte é a pé.

Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator, sair115m da caixa ou perder caixa própria suspendem. Save falhou: conserva etapa anterior; instalação conserva equipamento coletado e conclusão conserva11/11 pronto. Pendência suspensa bloqueia outras operações econômicas territoriais até concluir/cancelar. Trava global fica disponível a outras atividades após suspensão.

## Cenário e props

Após finalização nova confirmada, solicita **redCarpet** se ainda não estiver ativo. Prazo visual próprio de10minutos de jogo continua nos ticks mesmo fora do contrato, inclusive menus/outros trabalhos; pausa e descontinuidades maiores de2s não avançam. Nova conclusão real pode renovar o prazo próprio, replay não renova. Não concede renda adicional nem altera a validadeUTC do bônus.

Se o cenário **já estava ativo ao concluir**, esta atividade o usa sem assumir propriedade e **não o remove**. Quando ela solicita um cenário inativo, remove sua solicitação ao vencer o prazo ou fechar o script. Não persiste propriedade do IPL ao recarregar nem tenta remover cenário que encontrou ativo na nova sessão. O GTA expõe estado ativo, sem identificar solicitações posteriores de outros scripts; esse limite da API permanece.

Peças coincidentes de outros recursos são aproveitadas visualmente sem mover/apagar. Somente props criados por este contrato têm tokenDECOR e são removidos ao suspender/concluir. Props próprios sem colisão; posições/modelos/rumos seguem referência e instalação usa colocação no chão. A peça chamada Red Carpet na própria PSRP usa **prop_premier_fence_01**; o tapete real aparece com o **IPL redCarpet**, depois da confirmação final.

Respeita Visuals.Enabled/UseProps capturados na ativação: sem cosméticos, mantém coletas, viagens a pé, instalações, markers e provas; a finalização ainda solicita cenário como na referência. Sem alterar carros/NPCs/arquivosRPF, polícia, relações globais ou scripts do GTA. Animação só no jogador durante interação/carregamento; modelos carregam assincronamente com limite10s.

## Arquitetura

Novos GangTheatreData.cs/GangTheatre.cs e config **Gangs/Theatre.json**. Wallet.GangTheatre opcional/extensível: Pending Id/Gang/Area9/Options/Installed/Carrying; Accounts Gang/Area9/Completed/StartedUtcTicks/ExpiresUtcTicks;640 contas/256 recibos. Installed0…11 e Carrying apenas antes de completar. Sem handles/timers/IPL persistidos; legado consultado não ganha livro/bônus.

Begin/Collect/Install/Cancel/Complete clonam o livro e gravam uma única Wallet, com rollback/replay/posse/ordem/capacidade/overflow. Conclusão remove pendência e concede bônus atomicamente. Não altera banco/cash, inventário, estoque, receita suja, tesouraria, reputação/diplomacia ou MissionWin em outro arquivo. Bônus usa UTC real inclusive jogo fechado; nova conclusão renova sem somar duração, replay não renova e relógio anterior à concessão não ativa.

Opções BoostMinutes60(1…180), StartRadius8(3…15), HandoverMilliseconds1500(500…5000), InteractionDistance2,3(1…3), CrateModel/Crate e lista1…30 de itens únicos a3…100m da caixa. Defaults11 da PSRP. Config inválida usa defaults em memória e mantém original. Alterações afetam próximo contrato, sem mudar snapshot atual/modelos/pontos/tempo.

## Comparação e validação

PSRP/GangSpawner.cs StartRedCarpetSetup/PickupTheatreEquipment/InstallTheatreEquipment/UpdateRedCarpetSetup/CompleteRedCarpetSetup confirmam área9, caixa/rumo,11 itens/modelos/pontos/rumos, mão direita57005 e offset0,12/0,04/-0,03 com rotação0/90/0, a pé/2,3m/limite115m, IPL10min e bônus2XUTC60min. Código próprio; referência não executada.

Diferenças: provas persistidas de coleta/instalação, trocas ativas1,5s, finalE, END/retomada, props opcionais e limpeza própria; sem atualização MissionWin entre arquivos. HUD nativo simples. Fases8(interface/necessidades) e9(realismo dos trabalhos) continuam reservadas.

**2588 testes aprovados**(2545 anteriores +43 novos); build Release net48 sem erros/avisos. Casos de ordem/coleta/instalação, rollback/replay, snapshot/campos futuros, bloqueios/limites/UTC e lease do IPL existente/próprio/renovação/pausa/rollover. **GTA não executado na nuvem**: testar caixa/animação/posições das11 peças/acesso, finalE/tapete/expiração10min, END/reload parcial/pronto, cancelamento, outros mods e bônus anteriores.
