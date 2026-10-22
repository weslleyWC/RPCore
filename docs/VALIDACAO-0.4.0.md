# Validação — RPCore 0.4.0

- Referências: ScriptHookVDotNet Enhanced 1.1.0.6 e LemonUI SHVDN3 2.2, mantidas.
- Build Release net48: zero erros, zero avisos.
- Dados: 75 testes aprovados, zero falhas, zero ignorados. Inclui os 63 anteriores e 12 novos casos para recursos reais do radial/HUD, navegação circular, valores compactos e evolução de saves Needs antigos. Importação Sleep validada nos jogadores originais.
- Relatório: DataTests.trx.
- SHA256 RPCore.dll: `34dfd5e7c4fd5e652e20e0bd84076330f7d1b15345d4444324d05bdf545abe19`.
- PSRP.dll de referência: 2698816 bytes, SHA256 `21282fd01c4ad631cbd68424e514b9c54db99971df9c900b09cf72dea318f4cc`. Inspeção somente de leitura de tipos de interface/nomes de recursos; original permanece fora da distribuição. Implementação própria.
- OIV/DLC continuam originais; não houve edição de RPF/YTD, update.rpf ou dlclist.xml.
- Checklist de renderização, entrada, persistência e gameplay: INTERFACE-0.4.0.md.
- A execução do GTA e a igualdade visual com os prints precisam do teste do usuário; nenhuma captura de gameplay foi produzida aqui.

Instalador 0.4.0 executado duas vezes com PowerShell 7/Linux em diretório GTA simulado da 0.3.0: 82 arquivos existentes byte a byte preservados, DLL anterior em backup, nova DLL idêntica ao build e Interface.ini instalado. Relatório: installer-validation-0.4.0.json.
