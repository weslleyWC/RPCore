# RPCore 0.27.0 — fase 6.1I: equipe presencial e manutenção PDM

## Instalação

Baixe e extraia RPCore-update-0.27.0.zip. Abra o PowerShell na pasta que contém Install-RPCore.ps1 e execute:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Ajuste o caminho se necessário. Requer GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não instale PSRP.dll como segundo script: ela é referência de análise. Recursos OpenIV já instalados continuam utilizados. O instalador faz backup da DLL e conserva saves/configurações existentes. Acrescenta Jobs/PdmPremises.json somente se ausente; não precisa reinstalar .oiv/.rpf. Celular F2 e teleporte F11 preservados.

## O que mudou

F10 → Trabalhos → PDM → Equipe / salarios / melhorias permite contratar Zeladoria: padrão $650 do caixa da empresa, salário $130 por período de 300 segundos ativos. Mecânico e salários já existentes continuam. A oferta adicional é mesclada em memória, sem reescrever PdmStaff.json nem mudar salários de contratos existentes. Configuração personalizada com zeladoria própria é respeitada.

F10 → Trabalhos → PDM → Equipe presencial / manutencao abre os carros expostos e os serviços. Mecânico e zeladoria contratados aparecem perto da PDM, caminham até o carro selecionado e executam o serviço. Solicite próximo ao escritório público, feche o menu e acompanhe o funcionário, ficando até 30 metros do carro. O funcionário precisa estar contratado e sem salário atrasado; dívida com ex-funcionário também bloqueia atendimento.

Reparo leva oito segundos de trabalho após chegar; custa $1000, ou $500 com oficina adquirida e mecânico elegível. Usa os preços da sua configuração. Corrige motor/carroceria no próprio carro exposto, mantendo estoque/vaga e sujeira. Carro já intacto não precisa de reparo. A opção antiga de reparo no estoque continua disponível, recolhendo a unidade selecionada como antes.

Limpeza leva seis segundos após chegar, usa animação/esponja e zera a sujeira. Não cobra taxa por serviço; o salário da zeladoria continua devido. Só atende unidades expostas sujas, sem conceder carro ou mudar custo de aquisição.

A cada 60 segundos ativos de showroom, carros atribuídos recebem um nível de sujeira, até 15. O relógio é comum ao showroom, sem cobrança/acúmulo offline; pausa, missão e morte não contam. Cada nível reduz novas cotações em 0,5%, até 7,5%. Uma venda em atendimento ou serviço em andamento protege sua unidade desse avanço. Asea intacto sem divulgação: limpo $11200; nível 1 $11144; nível 5 $10920; nível 15 $10360. Limpar recupera a cotação. Financiamentos/vendas já contratados mantêm os valores salvos.

Saves antigos começam com sujeira zero; ela passa a acumular após esta atualização. PdmPremises.json permite EnableDirt=false ou intervalo de 60 a 3600 segundos. Desativar interrompe o avanço, sem apagar sujeira já salva. Não altera preferências antigas.

## Roteiro de teste no GTA

1. Com empresa adquirida e caixa disponível, contrate mecânico e zeladoria no menu de equipe. Volte à PDM, espere até cinco segundos e confira os dois NPCs/blips. Salários precisam estar pagos para usar serviços.
2. Exponha um carro pronto em uma vaga externa pelo menu Showroom / vagas / compradores. Espere cerca de um minuto ativo. Consulte nível/cotação e observe sujeira no carro. Outras unidades guardadas não devem sujar. Ficar longe pode recolher a representação, mas conserva nível/vaga.
3. Peça zeladoria para o carro sujo, feche o menu e acompanhe a caminhada/animação. Espere seis segundos perto do carro após a chegada. Confirme sujeira zero, cotação restaurada e caixa sem taxa adicional. O relógio da sujeira continua depois e poderá sujar novamente.
4. Exponha uma unidade com dano salvo, por exemplo após entrega cancelada/retomada; peça mecânico. Confirme oito segundos, motor/carroceria reparados, mesma vaga/placa/estoque e débito único de $1000. Com oficina, repita em outra unidade danificada e confira $500. Não danifique os temporários protegidos para preparar o teste.
5. Durante serviço, segure End por um segundo para cancelar, ou use Cancelar servico. Confira caixa e condição originais, sem efeito parcial. Teste se afastar/morrer/salário vencer; deve cancelar sem cobrar. Menu/telefone/pausa não completam o gesto. Se NPC não conseguir chegar, timeout de 120 segundos cancela sem taxa.
6. Reinicie/troque perfil antes de concluir: intenção pendente deve ser encerrada sem cobrança; carro/vaga/sujeira permanecem. Depois de concluir, reinicie e confira persistência sem nova cobrança. Não há salário/sujeira por horas offline.
7. Deixe vencer um salário e tente serviço: deve bloquear. Pague no menu de equipe e repita. Morte do NPC não apaga contratação/dívida; recriação própria após espera de aproximadamente 30 segundos, conforme disponibilidade do ponto.
8. Confira vendas à vista/financiadas, coleta no porto, carro-forte, retomada, locadora, retirada/aporte de caixa, garagem, F2 e teleporte F11. Uma operação física concorrente deve impedir o início do serviço.

## Referência e limites

A PSRP.dll foi examinada em SpawnMechanicPed, SpawnCleanerPed, CreateCleanerSequence e evolução de DirtLevel. Posições/modelos e animação de zeladoria vêm da referência; código, economia e serviços são próprios. A referência contém tarefas de piso/lixo; esta entrega limpa carros mediante solicitação, não implementa esse ciclo de lixeiras. Seguranças, reformas, câmeras/coberturas e outras diferenças da empresa ainda permanecem para próximas subentregas/refinamentos.

Não força portas/IPLs, não remove pedestres/carros do ambiente e não muda relações globais. Caminhos e interiores dependem dos recursos já instalados; teste inicialmente vagas externas. Não executamos GTA nesta nuvem. Build/testes e simulação do instalador estão em VALIDACAO.md. Fases 8 e 9 continuam reservadas para o refinamento combinado.
