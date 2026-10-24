# RPCore 0.30.0 — fase 6.1L: cofre administrativo e seguro PDM

## Instalação

Extraia RPCore-update-0.30.0.zip e abra o PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Ajuste para seu caminho. Requer GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não execute PSRP.dll como segundo script. Não precisa reinstalar .oiv/.rpf. Backup da DLL, saves/configurações conservados; acrescenta Jobs/PdmRisk.json somente se ausente. Celular F2 e teleporte F11 mantidos.

## Acesso e regras

F10 → Trabalhos → PDM → Cofre / seguro / ocorrencias. O cofre começa FECHADO e sem seguro, inclusive em saves antigos. Atualizar não causa perda nem cobrança. O cofre administrativo usa o caixa da empresa já existente; não cria outro saldo e não substitui a coleta de valores com carro-forte.

Abrir cofre é uma ação voluntária no escritório externo. Com ele aberto, a pelo menos 150m do escritório, caixa acima de $50000 e sem operação física PDM pendente, cada 120 segundos ativos consecutivos permite uma ocorrência. Padrão: 15% de chance por avaliação; perda de 20% do caixa no momento. Voltar interrompe a exposição; fechar elimina este risco. Depois de uma ocorrência, o cofre fecha automaticamente. Pausa, morte, missão/cutscene e tempo offline não acrescentam tempo. Abrir não concede dinheiro nem muda estoque.

Proteção: câmeras adquiridas = 30 pontos; cada segurança contratado/sem atraso = 35. Acerto de ex-funcionário suspende proteção dos guardas. Chance efetiva = teto(chance-base × (100-proteção)/100). No padrão: nenhum equipamento/equipe = 15%; câmeras = 11%; câmeras + um guarda = 6%; câmeras + dois guardas pagos = 0%. É proteção contratual, válida mesmo quando NPCs não estão renderizados longe da empresa. Não há bônus em vendas nem dinheiro por instalar proteção.

Seguro é OPCIONAL. Contratar paga agora pelo caixa e estabelece renovação automática a cada 1800 segundos de jogo ativo (30 minutos), com termos salvos na apólice. Não usa banco pessoal. Sem caixa suficiente na renovação, encerra cobertura, sem gerar dívida. Cancelar interrompe futuras renovações sem estorno. Trocar plano paga o prêmio novo completo e reinicia seu período, sem devolver o anterior. Alterar configuração não reescreve apólices já contratadas.

| Plano padrão | Prêmio inicial e por renovação | Cobertura por ocorrência |
| --- | --- | --- |
| Nível 1 | $10000 | $100000 |
| Nível 2 | $20000 | $200000 |
| Nível 3 | $35000 | $500000 |
| Nível 4 | $55000 | $1000000 |
| Nível 5 | $80000 | $2000000 |

Quando a perda real cabe no limite da apólice ativa, o caixa recebe exatamente a mesma perda de volta no mesmo checkpoint. Acima do limite, não há indenização parcial. Não existe botão para pedir dinheiro livre. Exemplo sem outros débitos: caixa $200000 sem seguro perde $40000 e fica $160000; contratando nível 1 antes, caixa passa a $190000 e uma perda de $38000 é restituída integralmente, mantendo $190000. Prêmios e perdas líquidas entram no resultado da empresa; indenização não é receita de venda.

## Roteiro de teste no GTA

1. Atualize e confira Cofre / seguro: fechado, sem apólice e sem cobrança nova. Afaste-se: não deve perder caixa. Estoque, salários, financiamentos e preferências anteriores devem permanecer.
2. No escritório, abra o cofre, confira descrição de risco e feche. Não deve cobrar. Abra novamente e se afaste 150m por menos de dois minutos; volte e confira exposição interrompida sem efeito parcial. F11/teleporte pode agilizar o deslocamento. O risco só se realiza em uma avaliação com sorteio favorável à ocorrência; não é garantido em todo período.
3. Para teste acelerado opcional, com jogo fechado edite apenas Jobs/PdmRisk.json: RiskPercent=100 e ExposureSeconds=60. Sem proteção isso garante ocorrência em um minuto ativo, com caixa acima do limiar; com câmeras/guardas a chance continua reduzida, e 100 pontos ainda dão zero. Use um perfil de teste se quiser preservar o caixa principal. Restaure a configuração depois. Não precisa apagar saves.
4. Sem apólice, confirme perda de 20% do caixa real, cofre fechado depois, registro no menu/livro e nenhum débito pessoal. Espere mais: cofre fechado não gera outra perda. Reinicie: não repete ocorrência/pagamento.
5. Contrate nível 1 no escritório: confira débito de $10000, limite e tempo restante. Repita exposição voluntária com perda abaixo de $100000: perda e restituição devem ser iguais, sem lucro e sem duplicar dinheiro após recarregar. Perda acima da cobertura fica sem indenização; não recebe parcial.
6. Confira proteção antes/depois de contratar/pagar um guarda e adquirir câmeras. Salário vencido ou acerto pendente remove os 35 pontos daquele guarda. Com câmeras e dois guardas pagos, risco deve ser zero mesmo com cofre aberto. A câmera ao vivo e a defesa local anteriores continuam.
7. Teste renovação: padrão 30 minutos ativos. Para acelerar, PeriodSeconds=300 afeta apenas apólice NOVA; termos de uma já contratada não mudam. Confirme débito único na virada; sem caixa suficiente, apólice deve sumir sem negativar banco/caixa. Cancelar evita próxima cobrança; trocar cobra plano novo completo sem estorno.
8. Reinicie/troque perfil, pause ou inicie missão: sem horas offline nem mistura de caixa/apólice entre perfis. Tempo salvo da apólice continua; cofre aberto permanece aberto até fechar/ocorrência. Não há seguro geral contra danos de veículos ou ataques de outros mods.
9. Repita showroom, manutenção/sujeira, segurança, monitor CCTV, vendas/financiamentos, porto, carro-forte, retomada, locadora, garagem, inventário, F2 e teleporte F11. Operação física PDM pendente suspende exposição; prêmio de apólice contratada ainda segue seu relógio ativo.

## Referência e diferenças

Analisados safeOpen/risco distante, SafeRobbery/InsurancePayout, SwitchInsurance e RegisterWages/InsurancePayment na PSRP.dll. A referência mostra cobertura integral condicionada ao limite e cancelamento por falta de fundos. Código próprio usa primeira cobrança imediata, relógio ativo de 30 minutos, exposição de dois minutos e sorteio estável por recibo. Não reproduz o uso incorreto de variável aleatória encontrado no evento de referência.

Nível 5 da referência mostra $80000 no menu, mas cobra $85000 no registro periódico e usa limite interno de $200 milhões. RPCore define explicitamente $80000/$2 milhões na configuração, com snapshot. Pontuação 30/35/35 vem da referência; aplicação da pontuação à chance e exigência de salários pagos são regras próprias.

Esta subfase é administrativa: não acrescenta prop/animação do cofre, NPC ladrão, missão de roubo, seguro de carro pessoal ou mudanças à polícia. Demais diferenças PDM permanecem; fases 8/9 mantidas. Veja VALIDACAO.md e PSRP-SYSTEMS-AUDIT-0.30.0.json. GTA não executado nesta nuvem.
