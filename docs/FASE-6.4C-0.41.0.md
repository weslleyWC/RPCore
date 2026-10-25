# RPCore 0.41.0 — fase 6.4C: habilitação M1 / motos

## Instalação e acesso

Extraia RPCore-update-0.41.0.zip da branch rpcore-entrega e abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Mantém GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não reinstala .oiv/.rpf nem executa PSRP.dll junto. Acrescenta Jobs/DmvM1.json quando ausente e conserva o Dmv.json da classe C, saves, personagens, seguros e teclas anteriores.

**F10 → DMV / habilitacoes C e M1 → Trocar para classe M1 / motos**. O mesmo botão permite voltar à C. A seleção só troca o menu, não o ped ou o perfil. Não fica disponível durante uma prova ativa; suspenda antes de trocar. Uma classe com dados inválidos é protegida e permite selecionar a outra classe.

Use GPS: DMV e contrate a pé no atendimento, sem procurado/missão/combate/outra interface/atividade reservada. M1 tem certificado/licença/tentativas/respostas/recibos próprios no mesmo perfil. Não recebe aprovação por já ter C e não apaga uma tentativa C suspensa. Banco é o saldo real compartilhado do perfil; cada classe paga suas taxas. Não há novos bloqueios aos trabalhos, seguro, garagens ou qualificação de piloto.

## Teoria M1

Taxa padrão **$30 do banco**, 15 questões próprias sobre condução de moto, capacete, passageiro, frenagem, curvas, visibilidade e piso molhado. Mínimo **12 acertos**. Não usa o gabarito da classe C nem as imagens de perguntas da PSRP.

Cada resposta é gravada antes de avançar. Pode suspender e retomar no DMV sem nova taxa. Aprovação libera o prático M1, sem conceder licença ainda. Reprovação encerra a tentativa e nova prova paga nova taxa. Antes da autorização inicial pode estornar; depois há retomada ou abandono explícito sem estorno. Esse comportamento e a proteção de overflow/rollback são os mesmos do DMV C, agora selecionando o livro da classe correta.

## Prático M1

Taxa padrão **$60 do banco**, cobrada somente com modelos, vaga/chão, moto e examinadora preparados. **PCJ 600 com examinadora na garupa**, sem invencibilidade. Esta é uma escolha própria de implementação; a referência não tem o exame M1 completo.

1. Entre como condutor na moto marcada e equipe um capacete. O jogo normalmente o coloca após aguardar sentado; também há o botão opcional **F10 → DMV → Vestir capacete de exame**, com a moto parada. Não exige cinto na moto.
2. Demonstre seta direita e depois esquerda, usando as teclas configuradas; pisca-alerta e falta de capacete não passam. Se controles veiculares estiverem desativados, o menu fornece demonstração das setas na moto parada.
3. Siga os **36 pontos em ordem**. A rota usa as coordenadas da classe C como base urbana; não é apresentada como uma rota M1 pronta da PSRP. Nos **três primeiros trechos/pontos**, limite de **15 km/h** para controle em baixa velocidade. Depois, limite de **40 km/h**. O limite atual aparece no status.
4. Pare completamente por pelo menos um segundo nos pontos **5, 17 e 29**. Os pontos só avançam com condutor, examinadora e capacete corretos, proximidade/altura e velocidade dentro do limite daquele trecho.
5. Retorne à vaga, estacione por dois segundos, desembarque próximo da moto e volte a pé ao DMV. **Receber habilitacao aprovada** emite a M1 sem outra cobrança.

Excesso acima do limite atual mais 5 km/h por três segundos gera falta; persistir gera novas faltas. Ficar sem capacete por cinco segundos também gera falta e impede avançar a rota enquanto ele for exigido. Danos acumulados geram faltas; cinco faltas, danos graves, perda/morte da examinadora/moto, procurado/missão, abandono ou deslocamento descontínuo reprovam. Uma queda pode tirar a examinadora da garupa e interromper o exame. Limite padrão de 20 minutos ativos, com pausa do jogo respeitada.

Não há pista fechada de cones/slalom, reconhecimento automático de placas/semafóros, teleporte, congelar jogador ou ignorar a polícia. A avaliação de baixa velocidade é feita nos trechos marcados da rota. Não use teleporte com a moto durante o exame.

## Capacete do atendimento e roupas salvas

O botão Vestir capacete só atua no condutor da moto de exame parada. Se já houver capacete, conserva-o. Ao fornecer um, registra o adereço/texture anterior do slot da cabeça e verifica o capacete nativo. Ao encerrar, restaura somente se ainda for o mesmo ped/modelo e exatamente o adereço fornecido. Alterações feitas por outro sistema não são substituídas.

O save de corpo do personagem recebe um filtro para excluir especificamente esse adereço temporário fornecido pelo atendimento, conservando o adereço anterior e todos os demais dados de corpo/equipamento. Isso impede que o autosave ou a troca de perfil grave o empréstimo como roupa permanente. Não tenta capturar/restaurar estado completo de capacetes ou roupas de outros mods. Capacete colocado normalmente pelo GTA continua sob o comportamento do jogo.

## Salvar, suspender e retomar

END ou Suspender / retomar depois mantém o pagamento e o progresso já gravado. A retomada presencial prepara moto/examinadora novamente, refaz a preparação de capacete/setas e continua do ponto salvo, conservando faltas, tempo, preço e regras contratados. Se o relatório final já estiver pronto, recebe a licença diretamente no atendimento. Não cobra novamente nem retoma automaticamente ao iniciar scripts.

Reprovar no prático mantém o certificado teórico M1; outra tentativa paga $60. Abandonar sem estorno não apaga documentos C. O mesmo ID de tentativa em C/M1 usa recibos de livros separados, sem crédito cruzado ou cobrança duplicada na mesma classe.

Moto e examinadora temporárias usam a proteção de atividade existente. Não podem ser registradas em garagem/seguro durante a reserva/retirada. Encerramento normal espera o veículo ficar vazio/parado e sem conexão estrangeira; não apaga o jogador nem veículo ocupado. Cleanup do abort só limpa/libera os atores próprios. F2/F11/J e os recursos instalados continuam.

## Configuração, compatibilidade e referência

Novo **Wallet.DmvM1** opcional; **Wallet.Dmv** continua sendo classe C. Campos antigos de categoria ausentes continuam C, sem capacete nem limite especial de pontos. APIs de transação existentes usam C por padrão; seleção explícita M1 altera somente DmvM1 e o banco correspondente. Nenhuma licença/documento é concedido automaticamente por migração ou instalação.

Jobs/DmvM1.json tem Category=1, RequireHelmet=true, RequireSeatbelt=false; preços 30/60, PassScore=12, SpeedKmh=40 e PointSpeeds {0:15,1:15,2:15}, com chaves de pontos em índice zero. Limites devem ficar entre 5 e o limite geral. M1 não aceita configuração exigindo cinto ou desligando o capacete; arquivo inválido usa padrão embutido sem sobrescrever seu conteúdo. Preço/regras são congelados na tentativa paga; mudanças afetam tentativas novas. Dmv.json C existente não é regravado; seus dados/pagamentos/rota continuam.

Comparados TheoryTests e campos HasClassM1PassCertificate/HasClassM1DrivingLicense em MultiplayerCharacter da PSRP.dll. M1 tem nome/preço teórico 30 e descrição Coming In Later Update, sem handler. As perguntas, prático PCJ/garupa, capacete e limites por trecho são código próprio; não se afirma que esses comportamentos já existam na referência. Assinaturas de GIVE_PED_HELMET, REMOVE_PED_HELMET e IS_PED_WEARING_HELMET conferidas no SDK/documentação nativa.

Classe A/caminhão, fiscalização/documento físico/importação automática e demais diferenças continuam pendentes. Fases 8 (interface/mecânicas) e 9 (todos os trabalhos) permanecem previstas. A C anterior continua coberta pelos testes e sem mudanças de preços/rota padrão.

## Testes dentro do GTA

1. Atualize tendo licença/tentativa C salva. Abra C: deve conservar nota, certificado/licença ou progresso. Troque para M1: começa independente, sem conceder documento ou cobrar. Confirme ida/volta entre as classes com tentativa suspensa.
2. Faça teoria M1, confira questões diferentes, aprovação com 12+ e reprovação abaixo. Suspenda/reinicie/responda novamente: conserva respostas e taxa; não altera C. Já ter C não libera prático M1 sem a teoria correspondente.
3. Contrate M1 com $60, entre na PCJ como condutor e confira examinadora na garupa. Sem capacete/seta exclusiva não deve passar a preparação. Teste capacete automático e botão do menu, incluindo modelo feminino/masculino/protagonista utilizado.
4. Com um adereço original, use o capacete do atendimento, aguarde autosave e suspenda/encerre. Confira retorno do adereço e roupa salva do personagem. Se outro sistema mudar o adereço durante o exame, encerramento não deve substituí-lo pelo backup antigo. Trocar perfil não deve aplicar capacete/adereço no novo ped.
5. Percorra os três primeiros pontos a até 15 km/h e depois os demais a até 40; confira avaliação do limite atual, paradas 5/17/29, estacionamento/desembarque e emissão. Use o condutor correto; não teleporte durante a rota. A física da PCJ com garupa precisa ser validada no seu jogo/mods.
6. Suspenda/retome depois de alguns pontos: pagamento, faltas/tempo e progresso devem permanecer. Queda/perda de examinadora/danos devem interromper/reprovar sem apagar teoria ou C. Repetir licença emitida não deve cobrar/duplicar.
7. Repita um exame C: Asterope, cinto, 50 km/h e rota continuam como antes. Confira seguro/garagem/fuel, personagens/retorno, moradia/storage, F2/F11/J e trabalhos; nenhum passou a exigir M1.

Build sem erros/avisos; 1490 testes passaram (40 novos). GTA não executado nesta nuvem. Auditoria PSRP-SYSTEMS-AUDIT-0.41.0.json e VALIDACAO.md registram a análise e os limites dos testes automáticos.
