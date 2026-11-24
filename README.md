# Diário Livre

Aplicativo desktop **offline-first**, gratuito e open source para professores da Educação Básica brasileira (Ensino Fundamental I, II e Médio) organizarem turmas, horários semanais, planos BNCC, notas, frequência escolar (regra LDB de 75% e 200 dias letivos), mapa de sala e conselhos de classe em ambiente bilíngue (`pt-BR` e `en-US`).

## Funcionalidades Principais

- **Painel de Turmas & Aulas de Hoje**: Visão consolidada do ano letivo, criação de turmas Polivalentes (Fundamental I) ou Especialistas (Fundamental II / Médio), prévia dos alunos em risco de frequência (LDB) e cards **Aulas de Hoje** com lançamento direto para a chamada.
- **Grade de Notas Multi-Período**: Suporte a regimes **Bimestral**, **Trimestral** e **Semestral**, colunas dinâmicas por avaliação, `Soma Simples` ou `Média Ponderada`, recuperação (`Substituir Menor Nota` ou `Média com Recuperação`), exportação de **planilha CSV** e **Boletim PDF**.
- **Chamada Rápida & Diário de Aula**: Registro ágil de presenças (`P` / `F` / `FJ`), aulas geminadas (`LessonCount`), chamada única diária para Fundamental I, **Conteúdo Ministrado**, **Atividade do Dia**, **Tarefa de Casa**, atalhos rápidos e alertas LDB em tempo real (`Normal`, `Atenção ≥ 20%`, `Crítico > 25%`).
- **Calendário Letivo (LDB 200 Dias)**: Acompanhamento da meta anual de dias letivos, importação de feriados nacionais brasileiros com 1 clique, registro de recessos, reposições e sábados letivos, além de resumo por bimestre/trimestre/semestre.
- **Planejamento, Grade e Mapa de Sala**:
  - **Grade Horária Semanal** (Segunda a Sábado) com atalho **Iniciar Chamada** conectado ao horário e turma.
  - **Planos de Aula (BNCC)** com código BNCC, objetivos, atividade planejada, tarefa de casa, controle de status (`Planejada` / `Aplicada` / `Adiada`) e ação **Aplicar na Chamada** com 1 clique.
  - **Espelho de Classe Visual** configurável (fileiras × colunas) com avatares, destaque de PDI e clique-para-mover/trocar carteiras.
- **Ficha do Aluno & Relatórios em Lote (PDF)**: Foto local WebP (256×256px), sinalizador PDI, observações pedagógicas por categoria, ficha individual em PDF, **Ata do Conselho de Classe (PDF)** da turma e **Boletins da Turma em Lote (PDF)**.
- **Privacidade LGPD, Backups e Personalização**:
  - **100% offline**: banco SQLite local (`diariolivre.db`), avatares (`avatars/`) e preferências (`ui-preferences.json`).
  - **PIN local opcional** (PBKDF2-SHA256 com salt aleatório de 128 bits) para bloquear rapidamente a tela.
  - **Snapshots automáticos** na inicialização (mantém os últimos 5 em `.zip`), além de exportação e restauração manual em `.zip`.
  - Tema **Claro / Escuro** e idioma **PT-BR / EN-US** persistidos automaticamente.

## Arquitetura (Clean Architecture)

- **`src/DiarioLivre.Core`**: Entidades de domínio, enums, interfaces e serviços puros (`GradeCalculator`, `AttendanceCalculator`, `SchoolCalendarCalculator`, `PinHasher`), sem dependências externas.
- **`src/DiarioLivre.Infrastructure`**: SQLite + EF Core (`DiarioLivreDbContext` + repositórios), migração idempotente e seeding inicial, avatares WebP via `SkiaSharp`, exportação CSV e relatórios PDF via `QuestPDF`, além de backup/snapshot `.zip` (`SqliteBackupService`).
- **`src/DiarioLivre.Desktop`**: Interface desktop multiplataforma em **Avalonia UI 11**, `CommunityToolkit.Mvvm`, DI (`Microsoft.Extensions.DependencyInjection`), recursos bilíngues `.resx` e atalhos de teclado (`Ctrl+1..7` para navegar, `Ctrl+S` para salvar).
- **`tests/DiarioLivre.Core.Tests`**: Suíte de testes unitários em **xUnit**.

## Como Compilar, Testar e Executar

Pré-requisito: **.NET 10 SDK**.

```bash
dotnet restore DiarioLivre.sln
dotnet build DiarioLivre.sln
dotnet test DiarioLivre.sln
dotnet run --project src/DiarioLivre.Desktop/DiarioLivre.Desktop.csproj
```

## Empacotamento Standalone (Windows x64)

```powershell
pwsh -File scripts/publish-windows.ps1
```

O executável portátil único é gerado em `publish/win-x64/`.

## Diretório de Dados Locais & Privacidade (LGPD)

Os dados escolares nunca são enviados à nuvem e ficam gravados localmente em:

- **Windows**: `%LOCALAPPDATA%\DiarioLivre`
- **Linux / macOS**: `~/.local/share/DiarioLivre`

Estrutura local: `diariolivre.db`, `avatars/`, `auto-backups/` e `ui-preferences.json`.

## Licença

Distribuído sob a licença **MIT**. Veja [LICENSE](LICENSE) para detalhes.
