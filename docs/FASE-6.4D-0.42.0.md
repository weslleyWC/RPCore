# RPCore 0.42.0 — fase 6.4D: habilitação A / caminhão com carreta

## Instalação e acesso

Baixe e extraia RPCore-update-0.42.0.zip da branch rpcore-entrega. Abra PowerShell na pasta que contém Install-RPCore.ps1:

```powershell
powershell -ExecutionPolicy Bypass -File .\Install-RPCore.ps1 -GtaPath 'C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V'
```

Mantém GTA V Legacy, SHVDN Enhanced 1.1.0.6 e LemonUI 2.2. Não reinstala .oiv/.rpf nem carrega PSRP.dll junto. Novo Jobs/DmvA.json é instalado quando ausente; configurações C/M1, saves, personagens e F2/F11 são conservados.

**F10 → DMV / habilitacoes C, M1 e A → Trocar para M1 → Trocar para A.** A seleção percorre C → M1 → A → C e só fica disponível sem prova ativa. Suspenda antes de trocar. Os documentos, pagamentos e pendências das três classes são independentes por perfil; banco é compartilhado. Classe inválida fica protegida, permitindo selecionar outra. Não exige C para contratar A nem acrescenta bloqueios aos trabalhos de caminhão existentes.

**GPS: DMV**, com A selecionada, aponta para o atendimento industrial perto do depósito LS GAS em Cypress Flats: (482, -2173, 5.92). O ícone DMV A é separado do atendimento C/M1. Contrate a pé, sem procurado, missão, combate ou outra atividade reservada.

## Teoria e contratação

Teoria padrão **$30 do banco**, 15 questões próprias sobre caminhões/articulação e mínimo **12 acertos**. Cada resposta é gravada. Pode suspender e retomar sem cobrar novamente; certificado A não emite licença nem substitui C/M1. Nova tentativa após reprovação paga nova taxa.

Prático padrão **$120 do banco**, com **Phantom, carreta trailers2 e examinadora no passageiro**. A taxa e a prova completas são escolhas próprias do RPCore: a PSRP só anuncia classe A como futura. O valor fica visível antes de contratar. Modelo, equipe, vaga/chão e resolução dos pontos de rua são preparados antes de uma nova cobrança. Se não houver vaga segura, libera apenas os atores próprios e não inicia cobrança nova. Há busca de 35 alternativas no pátio, sem remover veículos de terceiros.

## Como fazer o prático

1. Entre no Phantom marcado como motorista. Coloque cinto com a tecla configurada, demonstre seta direita e depois esquerda. Se controles veiculares estiverem desligados, há botões de demonstração no menu durante a prova.
2. A carreta começa **desengatada**. Após as setas, dê ré devagar até engatar fisicamente a carreta marcada. O GTA faz o engate quando o cavalo mecânico está corretamente alinhado. O RPCore verifica o vínculo exato entre os dois veículos; não teleporta nem engata por você.
3. Siga os **16 pontos industriais em ordem**, com examinadora, cinto e carreta engatada. Limite padrão **50 km/h**. Pare pelo menos um segundo nos pontos **4, 9 e 14**. Os anchors próprios são resolvidos para nós de rua do GTA antes da cobrança e guardados na tentativa; não são uma rota A funcional recuperada da PSRP. Roteamento, tráfego e espaço para o conjunto precisam de teste real.
4. Ao terminar, retorne à vaga marcada no pátio. Já na etapa de estacionamento e a até 40 m da vaga, demonstre **pelo menos 5 m de ré real**, com o conjunto engatado, condutor, examinadora e cinto. Não conta rodar parado ou dirigir para a frente.
5. Coloque o **centro da carreta** no círculo: até 4 m horizontalmente, diferença de altura até 3 m e orientação até 20 graus da vaga. Pare **os dois veículos** por dois segundos. O status informa a distância de ré e quando pode desengatar.
6. Use **F10 → DMV → Desengatar carreta do exame** ou o comando nativo de desengate. O botão só atua no par exato deste exame, parado e alinhado, depois de aprovar a ré. Soltar antes durante o trajeto tem tolerância de cinco segundos para corrigir; persistir reprova. O trailer precisa permanecer na vaga ao confirmar o desengate.
7. Pare, retire cinto, desembarque perto do caminhão e volte a pé ao atendimento industrial. **F10 → DMV → Receber habilitacao aprovada** grava a classe A sem taxa adicional.

Danos nos dois veículos entram na avaliação. Excesso acima do limite +5 km/h por três segundos ou falta de cinto por cinco segundos geram faltas repetidas. Cinco faltas, 30 minutos ativos, danos graves, procurado/missão, perda de veículo/examinadora, engate em carreta errada, outro caminhão levando a carreta, abandono ou deslocamento descontínuo reprovam. Pausa não conta tempo. Não use teleporte durante a prova.

## Suspensão, saves e limpeza

END ou Suspender guarda progresso já confirmado e libera a atividade; a retirada do conjunto próprio espera veículos vazios/parados e respeita engates com terceiros. Ao retomar no atendimento, não cobra outra vez: conserva pontos, faltas, tempo, taxa e regras pagas. Como prepara outro conjunto, exige repetir entrada, setas, engate, ré/alinhamento/desengate. A distância parcial de ré não é carregada entre conjuntos. Relatório aprovado pendente pode emitir direto, sem gerar caminhão novo. Nova taxa só após terminar/reprovar/abandonar e contratar outra tentativa.

Livro opcional Wallet.DmvA, sem migração de licenças C/M1. Enum de etapas anterior preservado; três provas físicas A são campos adicionais. Dados desconhecidos são conservados. Salvamento com falha não confirma emissão/cobrança; tentativa persistida continua retomável. Estorno só antes da autorização inicial; depois existe abandono explícito sem estorno. Nenhum modelo de jogador, save legado ou configuração de trabalho é substituído.

## Testes dentro do GTA

- Confira teoria A ($30/12 acertos), prático ($120) e ausência de débito quando pátio/modelo indisponível.
- Teste espaço no pátio, examinadora, alinhamento do engate e todos os 16 pontos, especialmente cruzamentos, pontes e altura do trailer. Envie ponto e imagem se um trecho não comportar o conjunto.
- Teste três paradas, cinto, setas, velocidade/danos, desengate precoce e carreta errada; depois ré real, alinhamento, parada dos dois veículos, desengate e emissão.
- Suspenda no percurso e na manobra, reinicie scripts/jogo e retome: saldo não deve cair novamente, pontos/faltas/tempo continuam, mas a nova carreta exige novas provas físicas. Confira emissão pendente após reload.
- Confira isolamento C/M1/A entre perfis, regressões C/M1, seguro, personagens/roupas, trabalhos LS GAS/logística, celular F2 e teleporte F11 fora da prova.

Build/testes de dados não executam GTA. Não há pista de cones, fiscalização nova, reconhecimento de semáforos/placas, documento físico/importação PSRP, câmera de espelhos nem garantia de rota livre de tráfego. Refinamento da interface/mecânicas permanece na fase 8 e dos trabalhos na fase 9.
