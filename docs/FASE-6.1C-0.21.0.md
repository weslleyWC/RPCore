# Fase 6.1C — RPCore 0.21.0: equipe, salários e melhorias PDM

Continuação solicitada após a logística da 0.20.0. Implementa gestão de quatro funções, salários por tempo ativo, pagamento pelo caixa, demissão/acerto, oficina e divulgação com efeitos reais. Mantém estoque/coleta/exposição/cliente/caixa existentes. Não declara a empresa inteira concluída nem antecipa o refinamento global das fases 8 e 9.

## Instalação

Feche o GTA, extraia RPCore-update-0.21.0.zip e abra o PowerShell **na pasta onde está Install-RPCore.ps1**:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Use seu caminho real. DLL pronta; instalador guarda backup da anterior. Preserva saves e configurações, acrescentando Jobs/PdmStaff.json só se ausente. Não apague saves/configurações. Mantém F2/celular, F11/teleporte, recursos instalados e progressos. Não reinstala nem altera OIV/YTD/RPF/DLC.

## Acesso e regras

**F10 → Trabalhos → PDM / empresa e entregas → Equipe / salarios / melhorias.** Consulte de qualquer lugar; contratar/pagar/demitir/melhorar exige estar a pé no escritório externo PDM, sem missão/procurado/combate e sem entrega/coleta da empresa pendente. Empresa precisa ser sua. Aporte capital pela gestão anterior se faltar caixa.

| Função | Contratação (caixa) | Salário por 5 minutos ativos | Efeito |
| --- | ---: | ---: | --- |
| Mecânico | 1500 | 250 | Com oficina adquirida e sem atraso: reparo do estoque pela metade |
| Divulgação júnior | 500 | 100 | +2% sobre cotação de venda após danos |
| Divulgação sênior | 1000 | 200 | +4%; exige júnior contratado |
| Diretor de divulgação | 2500 | 500 | +6%; exige sênior contratado |

Divulgação usa o **maior bônus de funcionário sem atraso**, sem somar níveis. Júnior/sênior/diretor continuam funcionários distintos e cada um recebe salário. Bônus é aplicado no checkpoint da venda real ao cliente; não gera dinheiro ou vende estoque automaticamente. O lucro positivo para conquista usa o valor efetivamente vendido, como no ciclo anterior.

| Melhoria | Custo do caixa | Efeito |
| --- | ---: | --- |
| Oficina | 5000, uma vez | Com mecânico elegível: reparo padrão de 1000 passa a 500 |
| Divulgação | 3000, uma vez | Acrescenta 2 pontos percentuais ao bônus do profissional elegível |

Não concedem benefício sem funcionário correspondente. Com diretor elegível e divulgação, bônus padrão máximo é 8%. Mecânico não dá bônus de venda. Reparar só atua na unidade do estoque empresarial, sem alterar veículos pessoais. Melhorias são econômicas persistentes nesta entrega; não instalam cenário/objetos nem NPCs funcionários no mapa.

## Salários, tempo e demissão

Cada contratação guarda ID próprio, função/nome, salário, duração do período, bônus e multa daquele momento. Configuração posterior não reescreve contrato existente. Funcionário recém-contratado começa em zero, sem herdar tempo de colegas.

Conta tempo enquanto o script recebe ticks com jogador vivo, fora de pausa/missão. Menus LemonUI sem pausa continuam sendo tempo ativo. Não cobra com jogo fechado, outro perfil ativo ou pausa; atrasos longos entre ticks são limitados a dois segundos por amostra, sem recuperar um período enorme de uma só vez. Progresso é gravado aproximadamente a cada cinco segundos, também ao acessar/mudar gestão, vender e salvar. Sem checkpoint após queda abrupta, até o intervalo ainda não gravado pode ser perdido. Não usa data do computador para cobrar meses offline.

A cada período completo, soma salário **devido**, sem debitar dinheiro automaticamente. Benefício daquele funcionário fica suspenso até pagar. Os demais salários continuam acumulando; atraso não demite ninguém automaticamente. Menu mostra dívida e segundos restantes; reabra para atualizar. Notifica ao passar de zero para salário vencido.

Pague cada funcionário pelo caixa da empresa; saldo exatamente igual ao devido é aceito. Não usa banco pessoal como cheque especial. Se o valor mudou desde que o menu foi aberto, atualiza a tela antes de cobrar. Sem dinheiro ou falha ao salvar, preserva o devido. Novas contratações ficam bloqueadas se houver salários vencidos ou acerto de ex-funcionários.

**Demitir** remove o vínculo, mas transfere ao acerto: salário vencido + proporcional do período atual arredondado para cima + multa padrão de 250. Não debita caixa nessa ação; a dívida permanece até **Quitar ex-funcionarios**. Exemplo: júnior após 450 segundos tem 100 vencidos + 50 proporcionais + 250 de multa = **400**. Durante acerto pendente, todos os benefícios ficam suspensos e novas contratações bloqueadas. Pagar o acerto reativa os demais funcionários elegíveis. Demita divulgação do maior nível para o menor: diretor, sênior, júnior. Melhorias adquiridas continuam suas.

Resultado da empresa agora subtrai salários/acertos **já pagos**, custos de contratação e melhorias, além dos custos/reparos anteriores. Dívida ainda não paga aparece separada no menu Equipe; não é apresentada como dinheiro já gasto. Histórico registra contratação, pagamento, demissão com acerto pendente e melhorias.

## Roteiro de teste no GTA

1. Vá ao escritório externo a pé e abra Equipe. Empresa antiga deve iniciar sem funcionários, dívidas ou melhorias; caixa e estoque anteriores ficam iguais.
2. Contrate **júnior por 500**. Confira redução de apenas 500 no caixa, banco pessoal igual e bônus de 2%. Tentar contratar sênior sem júnior deve falhar; contratação duplicada não deve cobrar.
3. Opcional: compre divulgação por 3000. Júnior passa a 4%. Com Asea pronto e sem danos, venda anterior de 11200 passa a **11648** com júnior + divulgação, ou **11424** apenas com júnior. Entregue pelo ciclo de cliente/chaves e confira crédito único no caixa. Esse valor considera funcionário ainda sem salário vencido na confirmação da venda.
4. Para testar oficina, contrate mecânico (1500) e adquira oficina (5000). Use uma unidade danificada do estoque: por exemplo, inicie entrega, danifique o carro, pare/saia e cancele. Reparar deve custar **500** com mecânico elegível. Item intacto não cobra de novo. Carros pessoais ficam iguais.
5. Jogue por pelo menos cinco minutos ativos após a contratação. Reabra Equipe: júnior deve 100, mecânico 250, proporcional ao tempo individual desde contratação. Bônus/desconto ficam suspensos para quem deve; reparo volta a 1000. Salários continuam vencendo durante entregas/coleta, mas pagamento espera encerrar esses contratos.
6. Volte a pé ao escritório, sem contrato, e pague. Confira baixa da dívida, saída exata do caixa e retorno do benefício. Repetir pagamento não debita novamente. Sem caixa suficiente, a dívida deve continuar.
7. Demita o funcionário e confira acerto proporcional + multa. Tente contratar com acerto aberto: deve bloquear. Quite, contrate de novo e veja um vínculo novo começando em zero. Para hierarquia, demita diretor antes de sênior/júnior.
8. Salve/recarregue e troque de perfil: equipe, melhorias, salários devidos e segundos persistem só no perfil correspondente; não cobra tempo offline. Confira regressão de caixa/saque, estoque antigo, coleta/exposição/venda, garagem pessoal, F2/F11 e outro trabalho.

Para teste mais rápido em uma instalação de teste, Jobs/PdmStaff.json aceita PeriodSeconds entre 60 e 3600. Esse valor vale **só para novas contratações**, porque empregados existentes preservam o período contratado. Não apague a carteira para alterar o prazo.

## Comparação e limites

Estudo de PSRP/PDMCompany.cs: StaffManagement.Opening, contratações de mecânico/júnior/sênior/diretor, RegisterWages, pagamentos no caixa e PDMWagesOwedToFiredStaff. Na referência, salários são lançados por sete dias empregados, com valores/dinâmica próprios; há funcionários físicos e diversos subsistemas adicionais.

RPCore usa implementação própria e valores da sua economia. Período de cinco minutos ativos, taxas percentuais, desconto de oficina, cobrança proporcional/multa e suspensão de benefícios são decisões desta implementação, não alegação de comportamento idêntico ao legado. Não importa automaticamente antigos contratos financeiros da PSRP.

Ainda faltam, em outras subentregas da PDM: demais funcionários e sua IA, showroom inteiro, serviços visuais/infraestrutura física, transporte de cinco carros, propriedade dos transportadores, financiamento/repossessão, aluguel e logística do cofre. A exposição unitária e a coleta Packer/TRFlat anteriores continuam. Fase 8 cobre interface/mecânicas dos indicadores/menus; fase 9 revisará todos os trabalhos, incluindo a empresa.

Arquivos principais: PdmStaffData.cs (regras/transações), PdmStaffSystem.cs (menus/tempo), Jobs/PdmStaff.json (padrões), PdmData.cs/Wallet.cs/PdmCompany.cs/Main.cs (integração de caixa/reparo/venda). Estado novo aditivo: Wallet.json/Pdm/Staff. Snapshots/restauração e recibos recentes seguem a arquitetura atual. Falha de gravação não deve cobrar, apagar dívida ou conceder melhoria parcialmente; tempo pendente tenta novamente com mesmo ID.

Validação automatizada: build net48 com Enhanced 1.1.0.6/LemonUI 2.2 sem erros/avisos; **783 testes aprovados**, incluindo 38 novos. Não executa GTA, menus/renderização ou convivência real de mods; o roteiro acima depende do seu teste.
