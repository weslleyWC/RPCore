# 0.86.0 — editor persistente de objetos, ícones e locais

**F11 → Map Builder / objetos e locais** ou menu centralF10. A referênciaMapBuilder474linhas salva locais/ícones por personagem; não contém editor de props. Esta entrega acrescenta o editor próprio e integra locais aos serviçosRPCore existentes. Não editaRPF/OIV/YMAP nem objetos do ambiente.

## Roteiro único no GTA

1. A pé, fora de procurado/missão/trabalho e sem pendências, abra **Novo mapa** e dê um nome. É rascunho: nada é gravado até **Salvar mapa e sair**. A edição reserva o gate de atividades.
2. **Adicionar objeto** → escolha`prop_roadcone02a`. O cone aparece3m à frente. Outros nomes base/DLC instalado podem ser digitados; ped/veículo/modelo inexistente ou estrutura com raio superior20m não é aceito/renderizado. Aguarde carregamento até10s; arquivo importado com modelo ausente é conservado, sem criar recurso falso.
3. No objeto, escolha **Manipular no mundo**:Num4/6 moveX;Num2/8 moveY;PgUp/PgDn moveZ;Q/E gira o rumo. Passo alterna0,1/1/5unidades por segundo. **Enter/F11 confirma**, **ESC/Backspace desfaz esse movimento**, **END descarta o rascunho inteiro**. Durante manipulação, teclas não abrem telefone/radial/loja por baixo. Não há câmera livre/teleporte.
4. Teste definirX/Y/Z/RX/RY/RZ por número(use ponto decimal), **Ajustar ao chão**, **Duplicar**, excluir um prop e renomear mapa. Só seleciona/edita props deste editor. Prévia permanece congelada e sem colisão.
5. **Adicionar ícone/serviço aqui**: escolha marcador, loja, combustível ou oficina. Edite nome, estilo(casa/garagem/armazém/loja/posto/padrão), sete cores, alcance curto e legenda. Ponto é criado onde você está; há prevenção de duplicata do mesmo tipo em menos5m.
6. **Salvar mapa e sair** grava biblioteca por perfil e carrega este mapa. Objetos carregam até120m e descarregam após160m, por padrão. Se marcou colisão, só cria o prop quando pessoas/veículos estiverem fora do volume de segurança; afaste-se do ponto para testar. Não materializa colisão em cima do jogador/veículo.
7. Teste criar outro objeto/alterar mapa e **Cancelar rascunho**: versão salva fica intacta. **Descarregar mapa** remove somente recursos próprios, conservando JSON. **Carregar/selecionar** restaura o mapa. Ícones de outros mods, inclusive nomes terminados_C, devem permanecer.
8. Ligue **Auto-carregar selecionado** se quiser restaurar esse mapa ao recarregar o script/trocar de perfil. Começa desligado. Cada perfil tem sua biblioteca; não copia mapas automaticamente entre protagonistas/perfis. Sem auto-carregar, escolhaCarregar após iniciar.
9. **Exportar JSON** grava em`RPCore/Maps/Exports/<nome>.json`, com nome simples sem extensão/caminho(letras/números/_/-). Nome existente é substituído com backup.bak. **Importar mapa JSON** cria cópia com IDs novos, preserva arquivo original e metadados; não executa scripts. Formato suportado éJSON1deste editor, até1MB por importação. Lista mostra até24arquivos; biblioteca padrão12mapas/64props/32pontos. Limites ajustáveis em`Editor/Defaults.json`, até24/128/64.
10. Teste salvar, reiniciar e verificar transformações, ícones, colisão e auto-carregamento. Se editarMapEditor.json fora do jogo enquanto há rascunho, o editor recusa sobrescrever: exporte rascunho, cancele e recarregue arquivos. JSON inválido/versão desconhecida é protegido; corrija/restaure.bak e reinicie o script antes de gravar novamente.

## Testar serviços criados

- **Loja:** depois de salvar/sair, vá a pé até1,8m eE. Usa itens, preços, peso/capacidade, pagamento e inventárioRPCore existentes. Ponto precisa continuar no mapa ativo; procurado bloqueia esse serviço personalizado.
- **Combustível:** comFuel habilitado, pare até4,5m, desligue motor e use sua tecla de abastecer atual. Mesmas regras de veículo, exclusões, tanque, preço e bombeamento existentes. Não há combustível grátis nem reescrita deFuel/Stations.json.
- **Oficina:** comGarages habilitado, motorista de carro/moto pessoal parado até4,5m, sem procurado/missão;E abre **reparo$500/lavagem$25** existentes. Teste saldo insuficiente e conservação de combustível. Não permite serviço em veículo de trabalho protegido. Personalização completa de peças/pintura daPSRP não é declarada entregue por esse ponto; segue como lacuna funcional na revisão final.

Pontos só ficam ativos depois de salvar/carregar o mapa; edição não é uma loja aberta. Descarregar/trocar perfil tira os serviços personalizados sem alterar locais originais. Não cria propriedade, renda ou recompensa.

## Dados, proteção e limites

Dados em`SaveData/<perfil>/MapEditor.json`, separados deWallet e dos arquivos originais de locais. Gravação atômica com.bak, cópia de rascunho, revisão do mapa contra sobrescrita obsoleta e assinatura do arquivo contra alteração externa. Import/export preserva campos futuros; IDs regenerados evitam colisões de cópias. Objetos anexados por outro sistema são recolhidos só após soltar; cleanup confere token original e não remove ambiente. Props ausentes/fora dos limites ficam no JSON e no log, podendo voltar após instalar DLC e recarregar mapa.

3229 testes aprovados, Release net48 sem erros/avisos. CobremCRUD/revisões/rollback, cancelamento sem mutação, import/remapeamento/metadados/formatos, limites/coordenadas/modelos/nomes seguros, rotação, pontos duplicados, arquivo real/bak/proteção contra corrupção e gate. Não háGTA neste ambiente: colocação/colisão/streaming/controle, símbolos reais, serviços/pagamentos e coexistência com mods precisam deste roteiro.

F2, teleporteF11, trabalhos, polícia, estúdio/Halloween, saves eOIV/RPF/YTD mantidos. Próximo bloco é revisão final de paridade. Diferenças como tuning completo/locais antigosPSRP não convertidos devem ser separadas de refinamento; fases8(interface/mecânicas) e9(trabalhos/missões) seguem reservadas.
