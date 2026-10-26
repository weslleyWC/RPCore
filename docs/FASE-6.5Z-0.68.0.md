# Fase6.5Z — entrega de documentos médicos — 0.68.0

## Instalação e início

Baixe RPCore-update-0.68.0.zip na branch rpcore-entrega, extraia e execute Install-RPCore.ps1 **da pasta extraída**, apontando para a pasta real do GTA Legacy que contém GTA5.exe. Mesmo procedimento anterior, SHVDN Enhanced1.1.0.6/LemonUI2.2 SHVDN3. Preserva saves/configs/F2/teleportF11 e recursos .oiv/.rpf/YTD/props instalados. PSRP.dll é referência de análise, não dependência para instalar junto.

1. Controle **território6** com sua gangue. F4 → Comercio dos territorios → seção da área6 → **GPS: medico / documentos**.
2. Vá a pé a até8m do médico(-940.9167,-337.9953,38.97752), seguro, sem procurado/combate/ragdoll/missão/cutscene/modal/atividade ou contrato territorial pendente.
3. Selecione **Entregar documentos medicos**. Gratuito, sem dinheiro/estoque/bônus ao iniciar.
4. Deixe livre o ponto do médico e **afaste-se2m** se estiver sobre ele. O NPC próprio aparece com cenário de prancheta quando o ponto está livre; não remove pedestres/carros de outros mods para abrir espaço.

## Coletar e transportar

**E a pé a até2m do médico** inicia a troca de**1,5s ativos**. Permaneça perto e seguro. Médico entrega/jogador recebe; papel prop_cs_documents_01 fica preso à mão direita. Só depois da troca salva **Collected**. Se abrir menu/pausa, entrar em veículo ou sair do alcance durante a troca, não confirma; tente E novamente.

O GPS muda ao **centro médico**(342.6898,-1397.448,32.50928). Pode viajar **a pé ou em veículo**, inclusive seu carro. A missão não fornece/exige/reserva/repara/apaga/move veículos. Os documentos ficam ocultos enquanto está em veículo e voltam à mão ao sair, para evitar recorte visual no volante.

Respeita Visuals.Enabled/UseProps capturados na ativação. Se props desativados, omite o papel cosmético mas exige a mesma coleta/viagem/entrega e comprovantes. Documento é carga de missão, não item de inventário pessoal ou dinheiro. Prop perdido com render habilitado suspende; a coleta salva permanece e pode ser retomada.

## Entregar e concluir

Saia do veículo no destino. **E a pé a até2m do ponto verde** inicia o gesto de entrega por1,5s ativos, sem NPC adicional no destino como na referência. Confirma **Delivered**, remove o papel próprio e salva a prova de entrega. **E novamente no ponto** conclui o contrato e libera a recompensa.

Se a gravação falhar, conserva o estado anterior: coleta pode ser repetida quando não salva, entrega conserva Collected e confirmação final conserva Delivered para tentar novamente. Não há pagamento parcial/repetido.

## Recompensa

Conclusão libera **processamento automático2X na área6 por60minutos reais**. Linha **Documentos / bonus2X** no F4 mostra validade e histórico; reabra para atualizar.

Não liga automático sozinho. Com receita suja e automático ativo na área6, seguro/com trava livre/sem pendências, padrão consome até$500 a cada aproximadamente30s ativos e envia$425 ao caixa num lote inteiro, mantendo15% de taxa e arredondamento cumulativo. Sem receita suja não cria renda. Contrato não paga banco/cash, estoque, tesouraria, reputação ou diplomacia diretamente.

Bônus das áreas1…6 são independentes por perfil/gangue/área e usam **máximo2X**, não multiplicam. Tempos/valores do manual e venda automática de armas permanecem. Produção exige posse/filiação/apto. Validade UTC inclui menus/pausa/GTA fechado, como na PSRP; produção só no jogo ativo, sem renda offline/catch-up. Expirado retorna ao normal. Outra entrega realmente concluída renova60min a partir da confirmação sem somar duração; replay/E repetido não estende. Relógio do GTA não renova eUTC anterior à concessão não ativa; não há relógio externo antitrapaça.

## Suspender, retomar e cancelar

END suspende. Morte/procurado/combate/ragdoll/missão/cutscene/perda de posse/filiação/perfil/ator/atividade incompatível suspendem. Médico indisponível/morto/em veículo antes da coleta ou documento visual perdido também suspendem. **Collected/Delivered já salvos não são apagados**; não há limite de tempo de viagem.

F4 mostra **Documentos pendentes**, retomada, GPS e cancelamento:

- **Sem coleta:** retome a pé no ponto do médico, com posse e em segurança. Recria apenas NPC/documento próprios necessários, mesmas opções/contrato.
- **Collected:** retome **a pé em qualquer local seguro**, com posse e sem outra atividade. Recria o papel próprio e aponta ao destino; **não refaz a coleta e não exige retornar ao médico**. Pode entrar no carro novamente para continuar.
- **Delivered:** retome a pé a até2m do destino e confirmeE; não recria médico/papel nem repete viagem.
- **Cancelar:** grátis, sem pagamento/estorno/prêmio parcial. Disponível sem posse/filiação em segurança; bônus anterior conserva vencimento original. Falha de save mantém estado anterior.

Pendência, inclusive suspensa, bloqueia novas operações econômicas territoriais/automáticos até concluir/cancelar; outras atividades podem usar a trava global após suspensão. Não sobrescreve contratos anteriores nem inventário/veículo pessoal.

## Checklist no GTA

Teste ponto livre/médico/prancheta/gestos/papel na mão, pausa/afastamento durante troca e coleta1,5s. Viaje no carro pessoal, verifique papel oculto/reaparecendo e carro intacto. Teste marker/entrega a pé/E final, END/reload antes/depois deCollect eDelivered, cancelamento sem posse, troca de perfil, props desativados/perdidos e convivência com outros trabalhos/bônus/manual/auto armas. **GTA não foi executado nesta nuvem**; acesso aos hospitais com recursos instalados, animação/bone/papel/visibilidade e timing precisam do teste do usuário.

## Arquivos e comparação com PSRP

Novos **GangMedicalData.cs**, **GangMedical.cs**, **Gangs/MedicalCourier.json**. Wallet.GangMedical opcional: Pending Id/Gang/Area6/Options/Collected/Delivered; contas Completed/StartedUtcTicks/ExpiresUtcTicks, até640 contas/256 recibos/campos futuros. Sem handles/props/timers persistidos; documento próprio é reconstruído da prova de coleta válida. Legado não ganha livro/opt-in ao consultar.

Begin/Collect/Deliver/Cancel/Complete clonam um livro e gravam numa Wallet. Delivered implica Collected; só final remove pendência+premia atomicamente, com rollback/replay/posse/limites/overflow. Guardas de Bulk/Arms/OtherFree das demais atividades bloqueiam pendência/invalidez sem recursão. Seis multiplicadores observam mesmoUTC e usam máximo2X, preservando85%/15%/tesouraria.

Config BoostMinutes60(1…180), StartRadius8(3…15), HandoverMilliseconds1500(500…5000), DoctorModel s_m_m_doctor_01, DocumentModel prop_cs_documents_01, Doctor/Destination válidos e separados por pelo menos20m em plano. Config inválida usa defaults em memória e conserva original; alterações só afetam próxima contratação, não snapshots/bônus anteriores.

Doctor/documents têm tokenDECOR próprio. Gestos e cenário só no NPC criado e no jogador durante interação. Papel próprio removido ao entregar/suspender; médico próprio fora de veículo pode ser removido, dentro de veículo é marcado para liberação natural sem ejetar. Nenhuma propriedade de carro pessoal, relação global/polícia/scriptGTA é alterada.

PSRP/GangSpawner.cs StartMedicalCourierRun/SpawnMedicalCourierDoctor/CollectMedicalCourierDocuments/CompleteMedicalCourierDelivery confirmam área6, médico/documento, coordenadas/rumo109,513, cenário prancheta, mão direita offset0,1, coleta/entrega a pé até2m e2XUTC1h. Código próprio, PSRP.dll não executada.

Diferenças: trocas ativas de1,5s e provas persistidasCollect/Delivered comE final em vez de flag/bônus instantâneo; coleta retomável sem handles, props opcionais e ocultos ao entrar em veículo; spawn/cleanup próprios protegidos, HUD nativo simples e sem MissionWin cruzando arquivos. Loja temporária/demais atividades/diálogo/guerras/polícia/refinamentos e fases8(interface/necessidades)/9(realismo dos trabalhos) continuam futuros.

**2468 testes aprovados**:2428 anteriores +40 novos. Build Release net48 zero erros/avisos. Auditoria e relatórios acompanham a atualização.
