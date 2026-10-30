# Validação0.104.4 — personagens0.102.2 e restante atual

3974 testes ativos aprovados, nenhum falhou/ignorado. Release net48 sem erros/avisos. DataTests-0.104.4.trx.3940casos da base0.102.2 e34de armazenamento0.104;73casos dos recursos de personagens removidos foram retirados junto com as classes obsoletas, não ignorados para esconder falhas.

Verificado CharacterSystem.cs byte-idêntico à0.102.2/6bae73d e Main.SwitchCharacter exatamente igual à função daquela base.456outros arquivos fonte/config byte-idênticos à0.104.3/254817d; Main só tem a restauração delimitada e versão, preservando a integração atual do armazenamento. InterfaceOptions/Settings só retiram a opção de entrada automática. CharacterData/BodyRuntime/Profile/Cfg/Wallet também idênticos à base0.102.2. Evidência/hashes/lista em CHARACTER-ROLLBACK-0.104.4.json. Helpers de entry/startup/handoff/registry e referências retirados; DLL não compila ped candidato.

Diagnóstico geral mantém leitura de fases históricas, sem executar o fluxo removido. Nenhuma varredura de NPC ou migração/limpeza de saves foi introduzida. A chave de preferência antiga permanece no arquivo existente, ignorada pelo runtime. Armazenamento/grade/mouse/armaria/celular/banco/portas e outros sistemas atuais conservados.

Instalador duas vezes sobre0.104.3 preserva 243 arquivos de dados/configs existentes byte a byte, incluindo personagem feminina/pendências/ActiveProfile/retorno/backups e armazenamento. Relatório installer-validation-0.104.4.json; DLL anterior em backup e nova confere, RPF externo de teste intacto.111 payloads JSON/INI e áudio anteriores comparados; CRC/manifestos/fonte/DLL/TRX/audit verificados no pacote final.

Não há GTA executado aqui. Fechar totalmente a sessão com duplicatas antes de instalar; testar lista manual/carregar/voltar pelo F10 como0.102.2, conferir personagem/itens/saldos e armazenamento atual. Roteiro PERSONAGENS-RESTAURADOS-0.104.4.md. Restauração exata solicitada não é prova de ausência de crash nativo em qualquer ambiente.
