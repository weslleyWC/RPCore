# RPCore 0.28.0 — fase 6.1J: segurança presencial da PDM

## Instalação

Extraia RPCore-update-0.28.0.zip e abra o PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Ajuste o caminho para sua instalação. Requer GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não execute PSRP.dll como segundo script; ela é usada para análise da referência. Não precisa reinstalar .oiv/.rpf. Instalador faz backup da DLL anterior, conserva saves/configurações e acrescenta Jobs/PdmSecurity.json somente se ausente. F2 celular e F11 teleporte continuam.

## Acesso e funcionamento

F10 → Trabalhos → PDM → Equipe / salarios / melhorias: contratar Seguranca 1 e Seguranca 2, pagar salários ou demitir. Cada contratação padrão custa $1300 do caixa da empresa. Salário de cada guarda: $260 a cada 300 segundos ativos, conforme período configurado na equipe. Nenhum débito automático do banco pessoal. Contratar é opcional; atualizar não contrata ninguém.

F10 → Trabalhos → PDM → Seguranca / postos presenciais: consultar situação, marcar GPS dos postos, abrir contratação/pagamento ou ordenar retorno dos dois guardas. O menu de equipe também oferece esse acesso.

Dois modelos/postos da PSRP: s_m_m_highsec_02 em (-36.68868, -1109.76, 26.43722), heading 147.7108; s_m_m_highsec_01 em (-60.66545, -1091.188, 26.57865), heading 89.74267. Spawn procura calçada livre até seis metros do ponto, sem apagar pedestres ou forçar interiores. Só aparecem perto da PDM, aproximadamente 180m do escritório, com empresa adquirida. Espere até cinco segundos para tentativa de chegada; local bloqueado mantém a contratação e aguarda vaga.

Com salário em dia e acertos de ex-funcionários quitados, ficam armados com carabina e blip verde. Salário atrasado ou acerto pendente suspende defesa, desarma o guarda e deixa blip branco. Pague e reabra/aguarde a atualização para reativar. Contratos antigos mantêm seus salários; nova oferta é mesclada em memória, sem regravar os arquivos de equipe anteriores. Os guardas não dão bônus de venda/reparo.

A defesa responde somente a NPC ambiente humano que esteja em combate contra você, mecânico, zeladoria ou outro guarda, com linha de visão e dentro do raio do posto (padrão 20m). Não dispara contra quem apenas passa, fica armado ou mira sem entrar em combate. Não escolhe jogador, equipe, polícia/SWAT/exército nem pedestre de missão/script. Guarda e alvo precisam continuar na área; engajamento dura no máximo 20 segundos antes de retorno a pé. Há espera de dez segundos após retorno/reativação para nova resposta. Não há inimigos gerados por este sistema.

Procurado, missão, cutscene, morte do personagem ou afastamento suspendem a presença dos guardas. Eles não eliminam estrelas nem atuam contra polícia. Ao voltar às condições normais, a contratação continua e os NPCs podem reaparecer. Morte de guarda não apaga contrato/dívida; recriação após 60 segundos, se houver ponto livre. Não são invulneráveis. Arma tem 120 munições e não é derrubada como recompensa; não há salário nem munição convertidos em dinheiro do jogador.

## Roteiro para testar no GTA

1. Perto do escritório, com empresa e caixa, contrate os dois guardas no menu de equipe. Confira débito total padrão $2600, dois contratos independentes, modelos/posições/blips e ausência de novo veículo no estoque/garagem. Aguarde chegada e retorno ao posto.
2. Abra Seguranca / postos presenciais, use GPS de cada guarda e confira postos/calçada sem sobrepor pedestres. Use Retornar guardas aos postos: devem obedecer a pé, sem teleporte. Confira que pedestres que passam não são perseguidos.
3. Após cerca de cinco minutos ativos, confirme salário devido $260 por guarda e suspensão/arma recolhida. Pague apenas um: só esse deve reativar; pague o segundo e confira ambos. Valores aparecem no caixa/livro de salários, sem débito bancário pessoal. Períodos personalizados continuam respeitados.
4. Demita um após tempo parcial: contratação some, NPC próprio é recolhido e o acerto proporcional vai para a dívida de ex-funcionário. Enquanto houver acerto, outro guarda também suspende. Quite o acerto e confira reativação. Contratação de novo custa normalmente; não duplica cargo.
5. Se ocorrer ataque de NPC ambiente junto ao posto, observe resposta e retorno; saindo do raio, perdendo linha de visão ou após 20 segundos devem interromper. Não há evento de ataque automático nesta versão. Se aparecer procurado, a segurança suspende em vez de combater polícia. Este comportamento precisa de teste real; testes automatizados verificam os critérios, não a IA do GTA.
6. Inicie missão/entre em cutscene, afaste-se ou morra: os recursos próprios devem ser recolhidos e contratação/salários preservados. Com cenário normal perto da PDM, devem voltar conforme disponibilidade do local. Morte de guarda aguarda aproximadamente 60 segundos; não deve soltar arma nem contratar de novo.
7. Troque de perfil/reinicie: não deve duplicar guarda, contratação ou pagamento. Só o perfil proprietário tem sua própria equipe. Não há salário acumulado durante horas offline.
8. Repita showroom, mecânico/zeladoria/sujeira, venda à vista/financiada, porto, carro-forte, retomada, locadora, garagem, F2 e teleporte F11. Segurança é serviço local de fundo e não ocupa um contrato de entrega.

## Configuração e diferenças da referência

Jobs/PdmSecurity.json define ofertas dos dois postos, modelos/pontos, DefenseRadius (10–30m), RespawnSeconds (30–300), EngagementSeconds (5–30) e EnableDefense. false mantém guardas contratados desarmados, sem defesa; salários continuam conforme contrato. Uma oferta de guarda já personalizada em PdmStaff.json tem prioridade e não é sobrescrita. Configuração inválida usa defaults em memória conservando original.

Análise estática da PSRP.dll: SpawnSecurity1Ped, SpawnSecurity2Ped, registro de salários e gestão dos dois cargos. A referência usa CarbineRifle, 20m, invulnerabilidade e grupos de relacionamento; o RPCore usa código próprio, NPC vulnerável, arma finita e seleção local explícita. Não altera relações globais nem apaga o pedestre mais próximo para criar funcionário.

Esta entrega não implementa roubo/seguro/CCTV, porcentagem de segurança com benefício econômico, reformas ou toda a empresa PDM. Fases 8 (interface/mecânicas) e 9 (refinamento dos trabalhos) continuam mantidas. Consulte VALIDACAO.md e PSRP-SYSTEMS-AUDIT-0.28.0.json. GTA não foi executado nesta nuvem.
