# Fase 6.1H — RPCore 0.26.0: showroom e atendimento PDM

Acrescenta atribuição persistente de estoque a vagas, carros físicos protegidos, identificação/preço próximo ao veículo e atendimento de comprador no escritório externo. Configura as **12 posições numeradas da exposição e sete do estacionamento** encontradas na referência. Não declara toda a PDM nem todos os efeitos/câmeras do showroom concluídos. Fases 8/9 mantidas.

## Instalar

Feche o GTA, extraia RPCore-update-0.26.0.zip e abra o PowerShell **na pasta onde está Install-RPCore.ps1**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Use seu caminho real. DLL pronta; backup da anterior. Preserva saves/configurações, acrescentando Jobs/PdmShowroom.json só se ausente. Não apague carteira/estoque. Mantém F2/celular e F11/teleporte; sem reinstalar/alterar OIV/YTD/RPF/DLC. Arquivos padrão anteriores da PDM ficam iguais.

## Expor o estoque

1. Empresa precisa ser sua. Tenha um carro **[PRONTO]**, com motor/carroceria >=300. Unidade ainda no porto exige coleta; danificada pode exigir reparo. Não concede carro grátis nem aumenta capacidade do estoque por ter vagas físicas.
2. Vá a pé ao escritório externo da PDM, sem missão/procurado/combate e sem outra operação empresarial pendente. **F10 → Trabalhos → PDM → Showroom / vagas / compradores**. Também há entrada própria no menu de trabalhos.
3. Escolha uma vaga livre e **Expor modelo / placa**. Usa exatamente essa posição/heading, com chão/collision verificados; não procura outra posição dentro de uma vaga numerada. Se ocupada, bloqueada ou interior indisponível, não confirma atribuição nem cobra.
4. Para testar primeiro, prefira **Patio 1–7**. Pelo estoque, **Expor em vaga externa** tenta automaticamente as posições externas livres, sem apagar ambiente. O carro continua no estoque e aparece **[EXPOSTO]**; fica trancado, congelado e protegido como representação de vitrine.
5. Atribuição persiste por perfil. Ao se afastar, recolhe temporários vazios/parados e mantém a vaga salva; ao voltar/recarregar perto da PDM, recria só se não existir o veículo reconhecido e a vaga estiver livre. Se bloqueada, aguarda próximo retry. A consulta mostra a atribuição mesmo enquanto a representação está pendente.
6. Perto dos carros, aparece nome/preço. Cotação segue condição salva e divulgação elegível. Não muda a interface/HUD global nesta fase.
7. **Recolher modelo** remove só esse temporário/atribuição e preserva o carro pago no estoque. **Recolher todos os carros expostos** atua nos próprios veículos/vagas, sem zerar estoque ou dinheiro. Se ocupado/andando, pare/saia primeiro.

### Vagas internas e acesso

Pontos/rumos são da PSRP. Slots numerados 9–11 usam verificação externa por estarem no nível inferior de referência; os demais classificados internos exigem interior pronto e piso detectado. O sistema **não abre portas à força, não ativa/desativa IPL global nem limpa os geradores/carros do ambiente**. Acesso físico depende dos recursos instalados e do estado do mundo.

Se não conseguir montar uma vaga interna, teste as externas e confira espaço/interior. Não registra um novo carro invisível só para preencher a tela. Uma atribuição já salva pode aguardar interior/chão/vaga disponíveis; é possível recolhê-la pelo menu. Alterar configuração/recursos pode exigir recolher e colocar novamente.

Capacidade empresarial continua sendo a de Pdm.json, padrão seis unidades. Dezenove posições não significam dezenove carros concedidos; só pode expor estoque que já possui.

## Atender um comprador

1. Com um carro fisicamente exposto e vazio, vá a pé ao escritório externo e abra a vaga: **Atender comprador / valor**. Usa turno físico compartilhado; precisa encerrar outro trabalho.
2. Gera comprador próprio em um ponto livre próximo e ele aguarda na entrada externa, com blip amarelo. Se sem espaço, afaste-se um pouco dentro da área de atendimento e tente novamente. Não pega pedestre aleatório do mundo.
3. O preço fica fixado ao iniciar esse atendimento, incluindo condição/divulgação daquele momento. Ainda não cobra nem retira o carro. Funcionários continuam com seus relógios; vencimento posterior do salário não reescreve a oferta já acordada.
4. Aproxime-se a pé/desarmado do comprador e pressione **E**. Aguarde três segundos sem se afastar/abrir menu/pausar. Carro exposto deve continuar reconhecido, parado e vazio; comprador vivo e perto da entrada.
5. Confirma venda em um checkpoint: crédito no caixa, receita/custo/venda, baixa exata do estoque, remoção da atribuição e encerramento da intenção. Asea intacto sem divulgação: **11200 no caixa**, custo vendido 8000 e margem 3200 antes dos demais gastos.
6. Depois do pagamento, destranca/descongela o próprio carro para o comprador entrar e sair. Se o embarque/saída do interior ficar bloqueado por mais de 20 segundos, recolhe apenas recursos próprios seguros, sem repetir nem desfazer a venda. Nunca apaga carro com jogador/outros ocupantes dentro; usa retirada posterior quando vazio.
7. Dinheiro vai ao caixa empresarial, não automaticamente à mão/banco pessoal. Saque e carro-forte continuam separados. Outros recebimentos simultâneos podem alterar saldo total; confira o histórico da venda específica.

End segurado ou Cancelar atendimento **antes do pagamento** remove só a intenção/cliente e mantém carro, vaga e estoque, sem receita. Morte/procurado/missão, perda do carro/comprador, afastar-se mais de 100 m ou prazo de dois minutos encerram o atendimento não pago. Menu/pausa/interrupção durante o gesto reinicia a passagem de chaves. Se caixa/counters bloquearem pagamento, pode cancelar, resolver caixa e iniciar outro atendimento.

Depois da venda paga, não permite cancelamento que devolva o mesmo carro ao estoque. Ao recarregar antes do pagamento, recupera intenção interrompida sem vender: a vaga e o bem continuam seus. Falha ao salvar restaura snapshot; replay não paga nem remove outro carro.

## Integração com a empresa

Exposto continua sendo estoque, mas não pode ser usado simultaneamente como entrega/financiamento ou fornecimento à locadora. Pelo estoque, entregar/reparar recolhe **somente a unidade selecionada**, preservando os demais carros expostos. Para locadora, recolha antes as duas unidades; ofertas contam apenas carros prontos sem vaga atribuída. Atendimento ativo bloqueia outra operação PDM conflitante.

Coleta do porto, retomada, cofre, pessoal e demais contratos seguem seus ciclos. Outros carros expostos podem continuar nas vagas enquanto há movimentação de unidades diferentes. Banco, inventário, licenças e Transport/garagem pessoal permanecem. Temporários de exposição são reservados no controle de trabalhos e não são carros pessoais armazenáveis.

O antigo atalho de exposição externa passa a usar vagas persistentes. A exposição unitária da 0.25.0 era temporária e não tinha atribuição em save; não converte veículo desconhecido do mundo em estoque. Nenhuma cobrança retroativa ou migração destrutiva.

## Estado, configuração e código

Campos aditivos **Pdm.Showroom** (SlotId/StockId/Handle) e **ShowroomSale** (ID/unidade/handle/preço). Ausentes = vazio/nenhum atendimento. Slots e unidades são únicos, handles positivos não se repetem. Atribuição/remoção não alteram saldos; venda junta todos os efeitos econômicos. Metadados de handle são atualizados ao recriar/recolher, sem dar baixa no bem. Campos desconhecidos são preservados.

Jobs/PdmShowroom.json: Slots com Id/Name/Point/Interior/Enabled, HandoverSeconds 3, BuyerTimeoutSeconds 120. Aceita até 24 posições configuradas e as 19 da referência vêm por padrão. Enabled=false impede novas colocações/respawn daquela vaga; recolha atribuições existentes para retirá-las imediatamente. Configuração inválida usa defaults embutidos sem apagar o arquivo do usuário.

PdmShowroomData.cs contém regras/transações/tempo; PdmShowroomSystem.cs, vagas/atores/consulta/preço/atendimento/cleanup. RigSpawn.FindDisplayVehicle acrescenta validação de **uma posição fixa**, sem mudar os métodos anteriores dos trabalhos. Corrigido uso do native do Enhanced para **GET_INTERIOR_AT_COORDS**. PdmCompany/Wallet/Main e guardas dos contratos integram o módulo; prepare/remove seletivo evita apagar toda a exposição ao entregar um único carro.

## Referência e diferenças restantes

Estudo de PSRP/PDMCompany.cs: ShowroomSpots/Slot1–12, SpawnVehicleInSlot, SpawnVehicleInParkingLot, LoadShowroomCars, PlaceShowroomVehicleOnGround e SellRandomPDMVehicleOnShowroomFloor. Comparadas as 19 coordenadas/headings. Código próprio.

Diferenças deliberadas: atendimento explícito de comprador na entrada externa e preço contratual fixado no início, em vez de venda automática aleatória por reputação. Sem apagar veículos do ambiente, forçar portas/interior, câmeras de preview, sistema de panos/coberturas ou reformas físicas. Proteção/congelamento limita-se aos próprios temporários expostos, sendo removida para a saída do comprador.

Permanecem outras subentregas PDM: funcionários/IA adicionais, reformas físicas, seguro/cofre interno/roubo, propriedade/transporte de cinco carros, variantes de retomada e demais diferenças de paridade. Refinamento global de interface/mecânicas segue na fase 8; todos os trabalhos na fase 9.

## Validação e GTA

Build net48 Enhanced 1.1.0.6/LemonUI 2.2 sem erros/avisos; **965 testes aprovados**, incluindo 31 novos. Cobrem atribuição/duplicação, handles/respawn, estoque antigo, preparo seletivo/locadora, atendimento/preço/dano/divulgação, pagamento/limites/cancelamento, rollback/replay/reload, campos desconhecidos e tempo/overflow. Auditoria estática confere posições/fluxo; instalador testado em diretório simulado. GTA não executado nesta nuvem.

Teste no GTA: pelo menos dois carros em vagas externas diferentes; posição/altura, placas/preços, reload/afastamento/retorno, vaga bloqueada sem apagar ambiente; recolher só um, vender ao comprador com E e crédito único, cancelamento e cliente embarcando; depois testar vagas internas conforme os recursos. Conferir entrega/reparo seletivos, locadora excluindo expostos, financiamentos/retomada/coletas/pessoal/garagem e F2/F11. Acesso/pisos/portas, IA e renderização precisam de validação na sua instalação.
