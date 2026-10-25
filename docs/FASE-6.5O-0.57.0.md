# RPCore 0.57.0 — fase 6.5O: relações e acordos entre gangues

## Instalação

Baixe e extraia RPCore-update-0.57.0.zip da branch rpcore-entrega. DLL pronta; feche o GTA e abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Mantém GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Preserva saves, configurações existentes, visuais .oiv/.rpf/.ytd, celular F2 e teleport do F11. Não instale PSRP.dll junto. Gangs/Diplomacy.json só é acrescentado quando ausente.

## Consultar as relações

Afiliado a uma gangue cadastrada, abra **F4 → Relacoes / acordos**. O menu mostra a opinião de cada outra gangue sobre a sua: **Respeito, Neutro, Hostilidade ou Guerra**, cargas roubadas, acordos, gasto acumulado e data do último acordo.

Cada direção é independente: a opinião de A sobre B não muda automaticamente a de B sobre A. Os dados são separados por perfil e par de gangues; sair e voltar à mesma filiação mantém o histórico. Gangue removida da configuração deixa de aparecer no menu, mas seu histórico não é apagado.

Na primeira ação, usa Relationships da definição da gangue rival em relação à sua, se houver exatamente uma entrada válida. Valores reconhecidos: Respect, Neutral, Hate e War. Ausência/valor desconhecido/duplicidade de caixa usa Neutral em memória, preservando o arquivo original. Depois da primeira ação, o histórico salvo prevalece sobre alterações futuras da definição.

**Guerra é o estado diplomático nesta fase.** Não inicia ataque automático nem altera relações globais dos NPCs/polícia. Os confrontos explícitos anteriores continuam funcionando. Respeito não impede selecionar uma gangue como alvo de roubo; roubá-la consome esse respeito.

## Consequência do roubo

No ciclo da fase 6.5N, começar o contrato ou abater os três NPCs ainda não altera a relação. Ao confirmar **E + revista de 2 segundos**, salva a carga e piora a opinião rival exatamente um nível:

**Respeito → Neutro → Hostilidade → Guerra → Guerra.**

A carga e a consequência são gravadas juntas na Wallet. Falha de gravação conserva as duas referências anteriores, sem pacote nem penalidade parcial. E repetido, reload ou retomada com carga recuperada não pioram de novo. O contador de cargas roubadas aumenta inclusive quando já está em Guerra.

Cancelar, morrer ou falhar depois de recuperar a carga não desfaz a ofensa. Entregar os suprimentos não aplica outra penalidade. Contratos antigos cujo pacote já estava confirmado não recebem penalidade retroativa. Contratos antigos ainda antes da revista recebem a consequência quando revistados nesta versão.

## Negociar

1. Conclua/cancele o roubo e conclua/estorne fretes pendentes. Suspendê-los conserva o contrato e continua bloqueando o acordo.
2. No menu, escolha **GPS da sua HQ**. Esteja a pé, humano, vivo, em segurança, sem procurado/combate/ragdoll/missão/cutscene/atividade, a até **5m do centro da sua HQ**. Não depende da presença opcional do líder.
3. Anote o banco. Selecione **Negociar com ... por $1000** para confirmar a compra. Debita somente o banco e sobe exatamente um nível: Guerra → Hostilidade → Neutro → Respeito.
4. Um novo acordo com a mesma gangue exige **24 horas do relógio do jogo** desde o anterior. O menu mostra a última data. Reabrir/reload/trocar filiação e voltar não reiniciam o prazo. Recuar o relógio não antecipa acordo. Avançar vários dias permite uma etapa, sem multiplicar compras automaticamente.
5. Em Respeito não aparece compra e a transação recusa cobrança. Saldo insuficiente, contrato/atividade, distância, prazo ou dados inválidos recusam sem débito.

O preço é custo definitivo de negociação: não vai à tesouraria, não compra pacotes, não remunera personagem/NPC, não concede reputação/patente ou posse. Cada seleção bem-sucedida grava débito e atitude na mesma Wallet, com recibo e rollback em falha. Não muda cash nativo/armas/veículo/vida/procurado/relógio.

## Configuração e dados

Novo **Gangs/Diplomacy.json**: AgreementPrice padrão 1000 (1…1000000), CooldownHours padrão 24 (1…168). Configuração inválida usa padrões em memória sem substituir o arquivo. Novas condições valem para o próximo acordo; não reprecificam histórico. Relógio/cooldown é o do GTA, não o computador.

Novo **Wallet.GangDiplomacy**, opcional: Accounts com Gang/Rival/Attitude/StolenLoads/Agreements/Paid/LastAgreementTicks e Receipts. Até 1024 pares distintos sem diferenciar maiúsculas/minúsculas e 256 recibos. Campos futuros preservados. Valores inválidos, duplicidades, limites e overflow bloqueiam conservando os dados. Wallet antiga não cria esse livro só por ser carregada/consultada.

Novo GangDiplomacyData.cs contém regras/transações puras; GangDiplomacy.cs contém menu e condições de mundo; GangSystem integra abertura/fechamento por perfil; GangRaidData.Collect e Wallet mantêm as assinaturas anteriores e acrescentam overload para relação inicial. A revista agora grava dois livros juntos. Demais saldos, estoque Dealer/warehouse/legal, receita suja, tesouraria, GangState e progressão permanecem nas rotinas anteriores.

## Referência PSRP e limites

Varredura de PSRP.GangSpawner: WorsenGangRelationship aplica exatamente a cadeia de quatro estados e é chamado em CompleteDrugSupplyRunDealerSearch. ImproveGangRelationship faz o caminho inverso, mas a PSRP o usa com **30% de chance após uma venda em lote**, não em acordo pago. ApplyRelationships também aplica grupos nativos.

**Negociação paga e cooldown são implementação própria do RPCore**, uma maneira explícita de recuperar a relação nesta etapa. Venda em lote, seu sorteio de 30%, aplicação global de IA/guerras e prêmio MissionWin continuam pendentes. Não se afirma equivalência completa. Auditoria PSRP-SYSTEMS-AUDIT-0.57.0.json registra origem, hashes, cadeias e diferenças. Fase 8 (interface e mecânicas de necessidades) e fase 9 (refinamento de todos os trabalhos) continuam reservadas.

## Teste no GTA

- Consulte as relações no F4, use o GPS, tente acordo longe/veículo/procurado/sem saldo: banco igual. Na sua HQ segura, confirme um acordo e confira débito de $1000 e apenas um nível melhor. Reabra/reload: mesmo histórico, sem débito duplicado. Tente novamente antes das 24h: recusa; depois do prazo: somente uma nova etapa.
- Num novo roubo, anote a relação. Início/abates: igual. Revista confirmada: um nível pior e carga salva. END/reload/retomar/entregar: sem segunda penalidade. Cancelar depois da revista: ofensa permanece, sem estoque novo. Antes da revista: cancelar não deve mudar relação.
- Teste contrato da versão anterior antes e depois da coleta. Depois da coleta antiga não deve inventar ofensa retroativa. Confira que perder carga após coleta não restaura a relação e que Guerra não cria ataques novos automaticamente.
- Confira separação entre perfis e filiações, banco/cash/estoques antigos, frete legal/criminal, venda/processamento/tesouraria, captura/defesa/iniciações, celular F2 e F11.

Build e testes de dados não executam GTA. Menu/input/GPS/raio da HQ, relógio nativo, integração com revista e convivência com mods precisam desta validação dentro do jogo.
