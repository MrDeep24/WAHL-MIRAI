// wwwroot/js/locale.js
// Handles language selection, ASP.NET Core culture cookie sync, and clean native DOM translation

(function () {
  const COOKIE_NAME = 'CurrentCulture';
  const ASPNET_COOKIE = '.AspNetCore.Culture';
  const SUPPORTED_LANGS = ['es', 'en', 'de', 'fr', 'ja', 'zh'];

  // Limpiar cualquier residuo previo de Google Translate
  try {
    document.cookie = "googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
    document.cookie = "googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=" + window.location.hostname;
    document.cookie = "googtrans=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/; domain=." + window.location.hostname;
  } catch (e) {}

  // Diccionario exhaustivo de navegación, botones y textos para Wahl Mirai
  const DICTIONARY = {
    en: {
      // Navegación
      "Panel de Control": "Dashboard",
      "Mis Candidaturas": "My Candidacies",
      "Votar": "Vote",
      "Resultados": "Results",
      "Ayuda": "Help",
      "Mi Perfil": "My Profile",
      "Salir": "Logout",
      "Cerrar sesión": "Logout",
      "Censo Electoral": "Electoral Census",
      "Reportes de Correo": "Email Reports",
      "Gestión PQR": "PQR Management",
      "Cuentas Administrativas": "Admin Accounts",
      "Gestión Electoral": "Electoral System",
      // Botones comunes
      "Guardar": "Save",
      "Guardar Cambios": "Save Changes",
      "Guardar cambios": "Save Changes",
      "Cancelar": "Cancel",
      "Cerrar": "Close",
      "Cerrar modal": "Close modal",
      "Enviar": "Send",
      "Enviar Respuesta": "Send Response",
      "Enviar respuesta": "Send Response",
      "Enviar enlace": "Send link",
      "Confirmar": "Confirm",
      "Confirmar voto": "Confirm vote",
      "Confirmar Voto": "Confirm Vote",
      "Emitir Voto": "Cast Vote",
      "Votar Ahora": "Vote Now",
      "Votar ahora": "Vote Now",
      "Ver propuestas": "View proposals",
      "Ver Propuestas": "View Proposals",
      "Propuestas": "Proposals",
      "Ver Resultados": "View Results",
      "Ver resultados": "View Results",
      "Iniciar sesión": "Sign In",
      "Iniciar Sesión": "Sign In",
      "Entrar": "Sign In",
      "Registrarse": "Register",
      "Crear cuenta": "Create account",
      "Recuperar contraseña": "Reset password",
      "Recuperar Acceso": "Access Recovery",
      "Descargar": "Download",
      "Descargar reporte": "Download report",
      "Descargar Reporte": "Download Report",
      "Descargar PDF": "Download PDF",
      "Descargar CSV": "Download CSV",
      "Exportar": "Export",
      "Exportar Censo": "Export Census",
      "Importar": "Import",
      "Importar Censo": "Import Census",
      "Subir archivo": "Upload file",
      "Examinar": "Browse",
      "Seleccionar archivo": "Select file",
      "Nueva Elección": "New Election",
      "Crear Evento": "Create Event",
      "Nuevo Evento": "New Event",
      "Agregar Candidato": "Add Candidate",
      "Añadir Propuesta": "Add Proposal",
      "Agregar Lista": "Add List",
      "Reintentar": "Retry",
      "Reintentar Envío": "Retry Sending",
      "Editar": "Edit",
      "Eliminar": "Delete",
      "Borrar": "Delete",
      "Ver detalles": "View details",
      "Detalles": "Details",
      "Buscar": "Search",
      "Filtrar": "Filter",
      "Limpiar filtros": "Clear filters",
      "Limpiar": "Clear",
      "Todas": "All",
      "Todos": "All",
      "Abierto": "Open",
      "Abiertos": "Open",
      "Resuelto": "Resolved",
      "Resueltos": "Resolved",
      "Activar": "Activate",
      "Finalizar": "Finish",
      "Pausar": "Pause",
      "Reanudar": "Resume",
      "Volver": "Back",
      "Anterior": "Previous",
      "Siguiente": "Next",
      "Continuar": "Continue",
      "Aceptar": "Accept",
      "Cambiar contraseña": "Change password",
      "Actualizar correo": "Update email",
      "Verificar": "Verify",
      "Copiar": "Copy",
      // Estados y etiquetas
      "Elecciones activas": "Active Elections",
      "Elecciones Pasadas": "Past Elections",
      "Total Electores": "Total Voters",
      "Participación": "Turnout",
      "Votos Emitidos": "Votes Cast",
      "Estudiante": "Student",
      "Administrador": "Administrator",
      "Super Administrador": "Super Admin",
      "Finalizada": "Finished",
      "Votado": "Voted",
      "Pendiente": "Pending",
      "Activa": "Active",
      "Borrador": "Draft",
      "Candidatos": "Candidates",
      "Voto en blanco": "Blank vote",
      "Voto en Blanco": "Blank Vote",
      "Blanco": "Blank",
      "Fecha de inicio": "Start date",
      "Fecha de fin": "End date",
      "Estado": "Status",
      "Acciones": "Actions",
      "No hay elecciones activas en este momento.": "No active elections at this time.",
      // Página de Inicio y Banner
      "Inicio": "Home",
      "El futuro de la": "The future of",
      "democracia escolar": "school democracy",
      "está en tus manos.": "is in your hands.",
      "está en tus": "is in your",
      "manos.": "hands.",
      "democracia": "democracy",
      "escolar": "school",
      "Wahl Mirai es la plataforma líder para la gestión de elecciones institucionales, garantizando el anonimato y la integridad de cada voto.": "Wahl Mirai is the leading platform for institutional election management, ensuring the anonymity and integrity of every vote.",
      "Empezar a Votar": "Start Voting",
      "Diseñado para la integridad institucional": "Designed for institutional integrity",
      "Nuestra arquitectura se centra en tres pilares fundamentales que garantizan un proceso electoral impecable.": "Our architecture focuses on three core pillars that guarantee a flawless electoral process.",
      "Voto Cifrado": "Encrypted Vote",
      "Utilizamos criptografía asimétrica para asegurar que tu voto permanezca anónimo y sea inalterable desde el momento de la emisión hasta el escrutinio.": "We use asymmetric cryptography to ensure your vote remains anonymous and tamper-proof from casting to tallying.",
      "Facilidad de Uso": "Ease of Use",
      "Una interfaz intuitiva que guía al estudiante paso a paso. Vota en menos de 60 segundos desde cualquier dispositivo móvil o de escritorio.": "An intuitive interface guiding students step by step. Vote in under 60 seconds from any mobile or desktop device.",
      "Resultados en Vivo": "Live Results",
      "Transparencia total. Los administradores y estudiantes pueden seguir el progreso de la participación y los resultados consolidados al instante.": "Total transparency. Administrators and students can track turnout and consolidated results in real time.",
      "Potenciando el liderazgo estudiantil a través de tecnología transparente, segura y accesible para todos.": "Empowering student leadership through transparent, secure, and accessible technology for everyone.",
      "Soporte": "Support",
      "Guía de Usuario": "User Guide",
      "Contacto": "Contact",
      "Términos de Uso": "Terms of Use",
      "Privacidad": "Privacy",
      "Cookies": "Cookies",
      "Legal": "Legal",
      "Todos los derechos reservados.": "All rights reserved.",
      "¿Necesitas ayuda?": "Need help?",
      "Visita la sección Ayuda para crear una PQR o leer las preguntas frecuentes.": "Visit the Help section to submit a ticket or read FAQs.",
      "Ir a Ayuda": "Go to Help",
      "Modo oscuro / claro": "Dark / light mode"
    },
    de: {
      // Navegación
      "Panel de Control": "Übersicht",
      "Mis Candidaturas": "Kandidaturen",
      "Votar": "Wählen",
      "Resultados": "Ergebnisse",
      "Ayuda": "Hilfe",
      "Mi Perfil": "Profil",
      "Salir": "Abmelden",
      "Cerrar sesión": "Abmelden",
      "Censo Electoral": "Wählerliste",
      "Reportes de Correo": "Mail-Berichte",
      "Gestión PQR": "PQR-Support",
      "Cuentas Administrativas": "Admin-Konten",
      "Gestión Electoral": "Wahlsystem",
      // Botones comunes
      "Guardar": "Speichern",
      "Guardar Cambios": "Änderungen speichern",
      "Guardar cambios": "Änderungen speichern",
      "Cancelar": "Abbrechen",
      "Cerrar": "Schließen",
      "Cerrar modal": "Schließen",
      "Enviar": "Senden",
      "Enviar Respuesta": "Antworten",
      "Enviar respuesta": "Antworten",
      "Enviar enlace": "Link senden",
      "Confirmar": "Bestätigen",
      "Confirmar voto": "Stimme bestätigen",
      "Confirmar Voto": "Stimme bestätigen",
      "Emitir Voto": "Wahl abgeben",
      "Votar Ahora": "Jetzt wählen",
      "Votar ahora": "Jetzt wählen",
      "Ver propuestas": "Vorschläge ansehen",
      "Ver Propuestas": "Vorschläge ansehen",
      "Propuestas": "Vorschläge",
      "Ver Resultados": "Ergebnisse",
      "Ver resultados": "Ergebnisse",
      "Iniciar sesión": "Anmelden",
      "Iniciar Sesión": "Anmelden",
      "Entrar": "Anmelden",
      "Registrarse": "Registrieren",
      "Crear cuenta": "Konto erstellen",
      "Recuperar contraseña": "Passwort zurücksetzen",
      "Recuperar Acceso": "Zugang wiederherstellen",
      "Descargar": "Download",
      "Descargar reporte": "Bericht laden",
      "Descargar Reporte": "Bericht laden",
      "Descargar PDF": "PDF laden",
      "Descargar CSV": "CSV laden",
      "Exportar": "Exportieren",
      "Exportar Censo": "Wähler exportieren",
      "Importar": "Importieren",
      "Importar Censo": "Wähler importieren",
      "Subir archivo": "Datei hochladen",
      "Examinar": "Durchsuchen",
      "Seleccionar archivo": "Datei wählen",
      "Nueva Elección": "Neue Wahl",
      "Crear Evento": "Event erstellen",
      "Nuevo Evento": "Neues Event",
      "Agregar Candidato": "Kandidat hinzufügen",
      "Añadir Propuesta": "Vorschlag hinzufügen",
      "Agregar Lista": "Liste hinzufügen",
      "Reintentar": "Wiederholen",
      "Reintentar Envío": "Erneut senden",
      "Editar": "Bearbeiten",
      "Eliminar": "Löschen",
      "Borrar": "Löschen",
      "Ver detalles": "Details",
      "Detalles": "Details",
      "Buscar": "Suchen",
      "Filtrar": "Filtern",
      "Limpiar filtros": "Filter löschen",
      "Limpiar": "Löschen",
      "Todas": "Alle",
      "Todos": "Alle",
      "Abierto": "Offen",
      "Abiertos": "Offen",
      "Resuelto": "Gelöst",
      "Resueltos": "Gelöst",
      "Activar": "Aktivieren",
      "Finalizar": "Beenden",
      "Pausar": "Pausieren",
      "Reanudar": "Fortsetzen",
      "Volver": "Zurück",
      "Anterior": "Zurück",
      "Siguiente": "Weiter",
      "Continuar": "Weiter",
      "Aceptar": "OK",
      "Cambiar contraseña": "Passwort ändern",
      "Actualizar correo": "E-Mail ändern",
      "Verificar": "Prüfen",
      "Copiar": "Kopieren",
      // Estados y etiquetas
      "Elecciones activas": "Aktive Wahlen",
      "Elecciones Pasadas": "Vergangene Wahlen",
      "Total Electores": "Wähler gesamt",
      "Participación": "Beteiligung",
      "Votos Emitidos": "Abgegebene Stimmen",
      "Estudiante": "Schüler",
      "Administrador": "Administrator",
      "Super Administrador": "Super-Admin",
      "Finalizada": "Beendet",
      "Votado": "Gewählt",
      "Pendiente": "Offen",
      "Activa": "Aktiv",
      "Borrador": "Entwurf",
      "Candidatos": "Kandidaten",
      "Voto en blanco": "Leere Stimme",
      "Voto en Blanco": "Leere Stimme",
      "Blanco": "Leer",
      "Fecha de inicio": "Startdatum",
      "Fecha de fin": "Enddatum",
      "Estado": "Status",
      "Acciones": "Aktionen",
      "No hay elecciones activas en este momento.": "Zurzeit keine aktiven Wahlen.",
      // Página de Inicio y Banner
      "Inicio": "Startseite",
      "El futuro de la": "Die Zukunft der",
      "democracia escolar": "Schuldemokratie",
      "está en tus manos.": "liegt in deinen Händen.",
      "está en tus": "liegt in deinen",
      "manos.": "Händen.",
      "democracia": "Demokratie",
      "escolar": "Schul-",
      "Wahl Mirai es la plataforma líder para la gestión de elecciones institucionales, garantizando el anonimato y la integridad de cada voto.": "Wahl Mirai ist die führende Plattform für institutionelle Wahlen, die Anonymität und Integrität jeder Stimme garantiert.",
      "Empezar a Votar": "Jetzt abstimmen",
      "Diseñado para la integridad institucional": "Entwickelt für institutionelle Integrität",
      "Nuestra arquitectura se centra en tres pilares fundamentales que garantizan un proceso electoral impecable.": "Unsere Architektur basiert auf drei Grundpfeilern, die einen einwandfreien Wahlprozess garantieren.",
      "Voto Cifrado": "Verschlüsselte Wahl",
      "Utilizamos criptografía asimétrica para asegurar que tu voto permanezca anónimo y sea inalterable desde el momento de la emisión hasta el escrutinio.": "Wir nutzen asymmetrische Kryptographie, damit Ihre Stimme von der Abgabe bis zur Auszählung anonym und unveränderlich bleibt.",
      "Facilidad de Uso": "Einfache Bedienung",
      "Una interfaz intuitiva que guía al estudiante paso a paso. Vota en menos de 60 segundos desde cualquier dispositivo móvil o de escritorio.": "Eine intuitive Benutzeroberfläche führt Schritt für Schritt. Wählen in unter 60 Sekunden von jedem Smartphone oder PC.",
      "Resultados en Vivo": "Live-Ergebnisse",
      "Transparencia total. Los administradores y estudiantes pueden seguir el progreso de la participación y los resultados consolidados al instante.": "Volle Transparenz. Administratoren und Schüler können die Wahlbeteiligung und konsolidierte Ergebnisse in Echtzeit verfolgen.",
      "Potenciando el liderazgo estudiantil a través de tecnología transparente, segura y accesible para todos.": "Förderung studentischer Führung durch transparente, sichere und zugängliche Technologie für alle.",
      "Soporte": "Support",
      "Guía de Usuario": "Benutzerhandbuch",
      "Contacto": "Kontakt",
      "Términos de Uso": "Nutzungsbedingungen",
      "Privacidad": "Datenschutz",
      "Cookies": "Cookies",
      "Legal": "Rechtliches",
      "Todos los derechos reservados.": "Alle Rechte vorbehalten.",
      "¿Necesitas ayuda?": "Brauchst du Hilfe?",
      "Visita la sección Ayuda para crear una PQR o leer las preguntas frecuentes.": "Besuche den Hilfebereich für Anfragen oder FAQ.",
      "Ir a Ayuda": "Zur Hilfe",
      "Modo oscuro / claro": "Dunkel- / Hellmodus"
    },
    fr: {
      // Navegación
      "Panel de Control": "Accueil",
      "Mis Candidaturas": "Candidatures",
      "Votar": "Voter",
      "Resultados": "Résultats",
      "Ayuda": "Aide",
      "Mi Perfil": "Profil",
      "Salir": "Déconnexion",
      "Cerrar sesión": "Déconnexion",
      "Censo Electoral": "Recensement",
      "Reportes de Correo": "Rapports e-mail",
      "Gestión PQR": "Support PQR",
      "Cuentas Administrativas": "Comptes admin",
      "Gestión Electoral": "Système électoral",
      // Botones comunes
      "Guardar": "Enregistrer",
      "Guardar Cambios": "Enregistrer",
      "Guardar cambios": "Enregistrer",
      "Cancelar": "Annuler",
      "Cerrar": "Fermer",
      "Cerrar modal": "Fermer",
      "Enviar": "Envoyer",
      "Enviar Respuesta": "Répondre",
      "Enviar respuesta": "Répondre",
      "Enviar enlace": "Envoyer lien",
      "Confirmar": "Confirmer",
      "Confirmar voto": "Confirmer le vote",
      "Confirmar Voto": "Confirmer le vote",
      "Emitir Voto": "Voter",
      "Votar Ahora": "Voter maintenant",
      "Votar ahora": "Voter maintenant",
      "Ver propuestas": "Voir propositions",
      "Ver Propuestas": "Voir propositions",
      "Propuestas": "Propositions",
      "Ver Resultados": "Résultats",
      "Ver resultados": "Résultats",
      "Iniciar sesión": "Connexion",
      "Iniciar Sesión": "Connexion",
      "Entrar": "Connexion",
      "Registrarse": "S'inscrire",
      "Crear cuenta": "Créer un compte",
      "Recuperar contraseña": "Mot de passe oublié",
      "Recuperar Acceso": "Récupérer accès",
      "Descargar": "Télécharger",
      "Descargar reporte": "Télécharger rapport",
      "Descargar Reporte": "Télécharger rapport",
      "Descargar PDF": "Télécharger PDF",
      "Descargar CSV": "Télécharger CSV",
      "Exportar": "Exporter",
      "Exportar Censo": "Exporter électeurs",
      "Importar": "Importer",
      "Importar Censo": "Importer électeurs",
      "Subir archivo": "Uploader fichier",
      "Examinar": "Parcourir",
      "Seleccionar archivo": "Choisir fichier",
      "Nueva Elección": "Nouvelle élection",
      "Crear Evento": "Créer événement",
      "Nuevo Evento": "Nouvel événement",
      "Agregar Candidato": "Ajouter candidat",
      "Añadir Propuesta": "Ajouter proposition",
      "Agregar Lista": "Ajouter liste",
      "Reintentar": "Réessayer",
      "Reintentar Envío": "Renvoyer",
      "Editar": "Modifier",
      "Eliminar": "Supprimer",
      "Borrar": "Supprimer",
      "Ver detalles": "Détails",
      "Detalles": "Détails",
      "Buscar": "Rechercher",
      "Filtrar": "Filtrer",
      "Limpiar filtros": "Réinitialiser",
      "Limpiar": "Effacer",
      "Todas": "Toutes",
      "Todos": "Tous",
      "Abierto": "Ouvert",
      "Abiertos": "Ouverts",
      "Resuelto": "Résolu",
      "Resueltos": "Résolus",
      "Activar": "Activer",
      "Finalizar": "Terminer",
      "Pausar": "Pause",
      "Reanudar": "Reprendre",
      "Volver": "Retour",
      "Anterior": "Précédent",
      "Siguiente": "Suivant",
      "Continuar": "Continuer",
      "Aceptar": "OK",
      "Cambiar contraseña": "Changer mot de passe",
      "Actualizar correo": "Changer e-mail",
      "Verificar": "Vérifier",
      "Copiar": "Copier",
      // Estados y etiquetas
      "Elecciones activas": "Élections en cours",
      "Elecciones Pasadas": "Élections passées",
      "Total Electores": "Total électeurs",
      "Participación": "Participation",
      "Votos Emitidos": "Votes exprimés",
      "Estudiante": "Élève",
      "Administrador": "Administrateur",
      "Super Administrador": "Super-Admin",
      "Finalizada": "Terminée",
      "Votado": "A voté",
      "Pendiente": "En attente",
      "Activa": "Active",
      "Borrador": "Brouillon",
      "Candidatos": "Candidats",
      "Voto en blanco": "Vote blanc",
      "Voto en Blanco": "Vote blanc",
      "Blanco": "Blanc",
      "Fecha de inicio": "Date début",
      "Fecha de fin": "Date fin",
      "Estado": "Statut",
      "Acciones": "Actions",
      "No hay elecciones activas en este momento.": "Aucune élection active actuellement.",
      // Página de Inicio y Banner
      "Inicio": "Accueil",
      "El futuro de la": "L'avenir de la",
      "democracia escolar": "démocratie scolaire",
      "está en tus manos.": "est entre vos mains.",
      "está en tus": "est entre vos",
      "manos.": "mains.",
      "democracia": "démocratie",
      "escolar": "scolaire",
      "Wahl Mirai es la plataforma líder para la gestión de elecciones institucionales, garantizando el anonimato y la integridad de cada voto.": "Wahl Mirai est la plateforme de référence pour la gestion des élections institutionnelles, garantissant l'anonymat et l'intégrité de chaque vote.",
      "Empezar a Votar": "Commencer à voter",
      "Diseñado para la integridad institucional": "Conçu pour l'intégrité institutionnelle",
      "Nuestra arquitectura se centra en tres pilares fundamentales que garantizan un proceso electoral impecable.": "Notre architecture repose sur trois piliers fondamentaux garantissant un processus électoral irréprochable.",
      "Voto Cifrado": "Vote chiffré",
      "Utilizamos criptografía asimétrica para asegurar que tu voto permanezca anónimo y sea inalterable desde el momento de la emisión hasta el escrutinio.": "Nous utilisons la cryptographie asymétrique pour garantir que votre vote reste anonyme et inaltérable de l'émission au dépouillement.",
      "Facilidad de Uso": "Facilité d'utilisation",
      "Una interfaz intuitiva que guía al estudiante paso a paso. Vota en menos de 60 segundos desde cualquier dispositivo móvil o de escritorio.": "Une interface intuitive guidant l'élève étape par étape. Votez en moins de 60 secondes depuis n'importe quel appareil.",
      "Resultados en Vivo": "Résultats en direct",
      "Transparencia total. Los administradores y estudiantes pueden seguir el progreso de la participación y los resultados consolidados al instante.": "Transparence totale. Administrateurs et élèves peuvent suivre la participation et les résultats consolidés en direct.",
      "Potenciando el liderazgo estudiantil a través de tecnología transparente, segura y accesible para todos.": "Renforcer le leadership étudiant grâce à une technologie transparente, sécurisée et accessible à tous.",
      "Soporte": "Support",
      "Guía de Usuario": "Guide d'utilisation",
      "Contacto": "Contact",
      "Términos de Uso": "Conditions d'utilisation",
      "Privacidad": "Confidentialité",
      "Cookies": "Cookies",
      "Legal": "Mentions légales",
      "Todos los derechos reservados.": "Tous droits réservés.",
      "¿Necesitas ayuda?": "Besoin d'aide ?",
      "Visita la sección Ayuda para crear una PQR o leer las preguntas frecuentes.": "Visitez la section Aide pour soumettre une demande ou lire la FAQ.",
      "Ir a Ayuda": "Aller à l'aide",
      "Modo oscuro / claro": "Mode sombre / clair"
    },
    ja: {
      // Navegación
      "Panel de Control": "ダッシュボード",
      "Mis Candidaturas": "立候補一覧",
      "Votar": "投票する",
      "Resultados": "結果",
      "Ayuda": "ヘルプ",
      "Mi Perfil": "プロフィール",
      "Salir": "ログアウト",
      "Cerrar sesión": "ログアウト",
      "Censo Electoral": "有権者名簿",
      "Reportes de Correo": "メール報告",
      "Gestión PQR": "問い合わせ",
      "Cuentas Administrativas": "管理者アカウント",
      "Gestión Electoral": "選挙システム",
      // Botones comunes
      "Guardar": "保存",
      "Guardar Cambios": "変更を保存",
      "Guardar cambios": "変更を保存",
      "Cancelar": "キャンセル",
      "Cerrar": "閉じる",
      "Cerrar modal": "閉じる",
      "Enviar": "送信",
      "Enviar Respuesta": "返信を送信",
      "Enviar respuesta": "返信を送信",
      "Enviar enlace": "リンク送信",
      "Confirmar": "確定",
      "Confirmar voto": "投票確定",
      "Confirmar Voto": "投票確定",
      "Emitir Voto": "投票する",
      "Votar Ahora": "今すぐ投票",
      "Votar ahora": "今すぐ投票",
      "Ver propuestas": "マニフェスト確認",
      "Ver Propuestas": "マニフェスト確認",
      "Propuestas": "マニフェスト",
      "Ver Resultados": "結果を見る",
      "Ver resultados": "結果を見る",
      "Iniciar sesión": "ログイン",
      "Iniciar Sesión": "ログイン",
      "Entrar": "ログイン",
      "Registrarse": "登録",
      "Crear cuenta": "アカウント作成",
      "Recuperar contraseña": "パスワード再設定",
      "Recuperar Acceso": "アカウント復旧",
      "Descargar": "ダウンロード",
      "Descargar reporte": "レポート出力",
      "Descargar Reporte": "レポート出力",
      "Descargar PDF": "PDF出力",
      "Descargar CSV": "CSV出力",
      "Exportar": "エクスポート",
      "Exportar Censo": "名簿出力",
      "Importar": "インポート",
      "Importar Censo": "名簿取込",
      "Subir archivo": "ファイル選択",
      "Examinar": "参照",
      "Seleccionar archivo": "ファイル選択",
      "Nueva Elección": "選挙作成",
      "Crear Evento": "選挙作成",
      "Nuevo Evento": "新規イベント",
      "Agregar Candidato": "候補者追加",
      "Añadir Propuesta": "公約追加",
      "Agregar Lista": "名簿追加",
      "Reintentar": "再試行",
      "Reintentar Envío": "再送信",
      "Editar": "編集",
      "Eliminar": "削除",
      "Borrar": "削除",
      "Ver detalles": "詳細",
      "Detalles": "詳細",
      "Buscar": "検索",
      "Filtrar": "絞り込み",
      "Limpiar filtros": "解除",
      "Limpiar": "クリア",
      "Todas": "全て",
      "Todos": "全て",
      "Abierto": "受付中",
      "Abiertos": "受付中",
      "Resuelto": "解決済",
      "Resueltos": "解決済",
      "Activar": "有効化",
      "Finalizar": "終了",
      "Pausar": "一時停止",
      "Reanudar": "再開",
      "Volver": "戻る",
      "Anterior": "前へ",
      "Siguiente": "次へ",
      "Continuar": "次へ",
      "Aceptar": "了解",
      "Cambiar contraseña": "パスワード変更",
      "Actualizar correo": "メール変更",
      "Verificar": "確認",
      "Copiar": "コピー",
      // Estados y etiquetas
      "Elecciones activas": "実施中の選挙",
      "Elecciones Pasadas": "過去の選挙",
      "Total Electores": "有権者数",
      "Participación": "投票率",
      "Votos Emitidos": "投票数",
      "Estudiante": "生徒",
      "Administrador": "管理者",
      "Super Administrador": "総管理者",
      "Finalizada": "終了",
      "Votado": "投票済",
      "Pendiente": "未投票",
      "Activa": "受付中",
      "Borrador": "下書き",
      "Candidatos": "候補者",
      "Voto en blanco": "白票",
      "Voto en Blanco": "白票",
      "Blanco": "白票",
      "Fecha de inicio": "開始日時",
      "Fecha de fin": "終了日時",
      "Estado": "状態",
      "Acciones": "操作",
      "No hay elecciones activas en este momento.": "現在、実施中の選挙はありません。",
      // Página de Inicio y Banner
      "Inicio": "ホーム",
      "El futuro de la": "未来の",
      "democracia escolar": "学校民主主義は",
      "está en tus manos.": "あなたの手の中にあります。",
      "está en tus": "あなたの手の中に",
      "manos.": "あります。",
      "democracia": "民主主義",
      "escolar": "学校",
      "Wahl Mirai es la plataforma líder para la gestión de elecciones institucionales, garantizando el anonimato y la integridad de cada voto.": "Wahl Miraiは、投票の匿名性と完全性を保証する、組織・学校向けの次世代選挙管理プラットフォームです。",
      "Empezar a Votar": "投票を始める",
      "Diseñado para la integridad institucional": "組織の信頼性と公正さのための設計",
      "Nuestra arquitectura se centra en tres pilares fundamentales que garantizan un proceso electoral impecable.": "完璧な選挙プロセスを保証する3つの基本柱に基づいた設計です。",
      "Voto Cifrado": "暗号化投票",
      "Utilizamos criptografía asimétrica para asegurar que tu voto permanezca anónimo y sea inalterable desde el momento de la emisión hasta el escrutinio.": "非対称暗号化を採用し、投票から開票まで投票内容の匿名性と改ざん防止を徹底しています。",
      "Facilidad de Uso": "直感的で使いやすい",
      "Una interfaz intuitiva que guía al estudiante paso a paso. Vota en menos de 60 segundos desde cualquier dispositivo móvil o de escritorio.": "直感的な操作画面で迷わず投票。PCやスマートフォンから60秒以内で投票完了できます。",
      "Resultados en Vivo": "リアルタイム結果",
      "Transparencia total. Los administradores y estudiantes pueden seguir el progreso de la participación y los resultados consolidados al instante.": "完全な透明性。管理者と生徒は投票率と集計結果をリアルタイムで確認できます。",
      "Potenciando el liderazgo estudiantil a través de tecnología transparente, segura y accesible para todos.": "誰にでも使いやすく透明で安全な技術を通じて、次世代のリーダーシップを後押しします。",
      "Soporte": "サポート",
      "Guía de Usuario": "ユーザーガイド",
      "Contacto": "お問い合わせ",
      "Términos de Uso": "利用規約",
      "Privacidad": "プライバシー",
      "Cookies": "クッキー",
      "Legal": "法的情報",
      "Todos los derechos reservados.": "全著作権所有。",
      "¿Necesitas ayuda?": "お困りですか？",
      "Visita la sección Ayuda para crear una PQR o leer las preguntas frecuentes.": "サポートセクションから問い合わせまたはよくある質問を確認できます。",
      "Ir a Ayuda": "ヘルプを見る",
      "Modo oscuro / claro": "ダーク/ライトモード切替"
    },
    zh: {
      // Navegación
      "Panel de Control": "控制面板",
      "Mis Candidaturas": "我的参选",
      "Votar": "前往投票",
      "Resultados": "选举结果",
      "Ayuda": "帮助中心",
      "Mi Perfil": "个人资料",
      "Salir": "退出登录",
      "Cerrar sesión": "退出登录",
      "Censo Electoral": "选民名册",
      "Reportes de Correo": "邮件报告",
      "Gestión PQR": "工单管理",
      "Cuentas Administrativas": "管理账户",
      "Gestión Electoral": "选举系统",
      // Botones comunes
      "Guardar": "保存",
      "Guardar Cambios": "保存更改",
      "Guardar cambios": "保存更改",
      "Cancelar": "取消",
      "Cerrar": "关闭",
      "Cerrar modal": "关闭",
      "Enviar": "发送",
      "Enviar Respuesta": "发送答复",
      "Enviar respuesta": "发送答复",
      "Enviar enlace": "发送链接",
      "Confirmar": "确认",
      "Confirmar voto": "确认投票",
      "Confirmar Voto": "确认投票",
      "Emitir Voto": "提交选票",
      "Votar Ahora": "立即投票",
      "Votar ahora": "立即投票",
      "Ver propuestas": "查看主张",
      "Ver Propuestas": "查看主张",
      "Propuestas": "竞选主张",
      "Ver Resultados": "查看结果",
      "Ver resultados": "查看结果",
      "Iniciar sesión": "登录",
      "Iniciar Sesión": "登录",
      "Entrar": "登录",
      "Registrarse": "注册",
      "Crear cuenta": "创建账户",
      "Recuperar contraseña": "找回密码",
      "Recuperar Acceso": "恢复访问",
      "Descargar": "下载",
      "Descargar reporte": "下载报告",
      "Descargar Reporte": "下载报告",
      "Descargar PDF": "下载PDF",
      "Descargar CSV": "下载CSV",
      "Exportar": "导出",
      "Exportar Censo": "导出名册",
      "Importar": "导入",
      "Importar Censo": "导入名册",
      "Subir archivo": "上传文件",
      "Examinar": "浏览",
      "Seleccionar archivo": "选择文件",
      "Nueva Elección": "新建选举",
      "Crear Evento": "创建选举",
      "Nuevo Evento": "新建活动",
      "Agregar Candidato": "添加候选人",
      "Añadir Propuesta": "添加主张",
      "Agregar Lista": "添加名册",
      "Reintentar": "重试",
      "Reintentar Envío": "重新发送",
      "Editar": "编辑",
      "Eliminar": "删除",
      "Borrar": "删除",
      "Ver detalles": "详情",
      "Detalles": "详情",
      "Buscar": "搜索",
      "Filtrar": "筛选",
      "Limpiar filtros": "重置筛选",
      "Limpiar": "清除",
      "Todas": "全部",
      "Todos": "全部",
      "Abierto": "进行中",
      "Abiertos": "进行中",
      "Resuelto": "已解决",
      "Resueltos": "已解决",
      "Activar": "激活",
      "Finalizar": "结束",
      "Pausar": "暂停",
      "Reanudar": "继续",
      "Volver": "返回",
      "Anterior": "上一步",
      "Siguiente": "下一步",
      "Continuar": "继续",
      "Aceptar": "确定",
      "Cambiar contraseña": "修改密码",
      "Actualizar correo": "修改邮箱",
      "Verificar": "验证",
      "Copiar": "复制",
      // Estados y etiquetas
      "Elecciones activas": "进行中选举",
      "Elecciones Pasadas": "历史选举",
      "Total Electores": "选民总数",
      "Participación": "投票率",
      "Votos Emitidos": "已投票数",
      "Estudiante": "学生",
      "Administrador": "管理员",
      "Super Administrador": "超级管理员",
      "Finalizada": "已结束",
      "Votado": "已投票",
      "Pendiente": "待投票",
      "Activa": "进行中",
      "Borrador": "草稿",
      "Candidatos": "候选人",
      "Voto en blanco": "投空白票",
      "Voto en Blanco": "投空白票",
      "Blanco": "空白票",
      "Fecha de inicio": "开始时间",
      "Fecha de fin": "结束时间",
      "Estado": "状态",
      "Acciones": "操作",
      "No hay elecciones activas en este momento.": "目前暂无进行中的选举活动。",
      // Página de Inicio y Banner
      "Inicio": "首页",
      "El futuro de la": "引领未来的",
      "democracia escolar": "校园民主",
      "está en tus manos.": "尽在你的掌握之中。",
      "está en tus": "掌握在你的",
      "manos.": "手中。",
      "democracia": "民主",
      "escolar": "校园",
      "Wahl Mirai es la plataforma líder para la gestión de elecciones institucionales, garantizando el anonimato y la integridad de cada voto.": "Wahl Mirai 是机构选举管理的领先平台，全面保障每一张选票的匿名性与真实完整。",
      "Empezar a Votar": "开始投票",
      "Diseñado para la integridad institucional": "专为机构公正与诚信打造",
      "Nuestra arquitectura se centra en tres pilares fundamentales que garantizan un proceso electoral impecable.": "我们的系统立足于三大核心支柱，确保选举流程万无一失。",
      "Voto Cifrado": "加密投票",
      "Utilizamos criptografía asimétrica para asegurar que tu voto permanezca anónimo y sea inalterable desde el momento de la emisión hasta el escrutinio.": "我们采用非对称加密技术，确保从投票到计票全程匿名且不可篡改。",
      "Facilidad de Uso": "简单易用",
      "Una interfaz intuitiva que guía al estudiante paso a paso. Vota en menos de 60 segundos desde cualquier dispositivo móvil o de escritorio.": "直观便捷的界面引导学生轻松操作。无论在电脑还是手机上，60秒内即可完成投票。",
      "Resultados en Vivo": "实时选举结果",
      "Transparencia total. Los administradores y estudiantes pueden seguir el progreso de la participación y los resultados consolidados al instante.": "公开透明。管理员与学生均可实时查看投票参与率与统计结果。",
      "Potenciando el liderazgo estudiantil a través de tecnología transparente, segura y accesible para todos.": "以透明、安全且触手可及的科技力量，赋能学生领袖的诞生与发展。",
      "Soporte": "支持与帮助",
      "Guía de Usuario": "使用指南",
      "Contacto": "联系我们",
      "Términos de Uso": "使用条款",
      "Privacidad": "隐私政策",
      "Cookies": "Cookie政策",
      "Legal": "法律信息",
      "Todos los derechos reservados.": "版权所有。",
      "¿Necesitas ayuda?": "需要帮助吗？",
      "Visita la sección Ayuda para crear una PQR o leer las preguntas frecuentes.": "前往帮助中心提交工单或查看常见问题解答。",
      "Ir a Ayuda": "前往帮助",
      "Modo oscuro / claro": "深色/浅色模式切换"
    }
  };

  function setCultureCookie(culture) {
    const value = `c=${culture}|uic=${culture}`;
    const days = 365;
    const d = new Date();
    d.setTime(d.getTime() + (days * 24 * 60 * 60 * 1000));
    const expires = "expires=" + d.toUTCString();
    
    // Cookie configurada en Program.cs
    document.cookie = `${COOKIE_NAME}=${value};${expires};path=/;SameSite=Lax`;
    // Cookie estándar de ASP.NET Core
    document.cookie = `${ASPNET_COOKIE}=${value};${expires};path=/;SameSite=Lax`;

    try {
      localStorage.setItem('user_locale', culture);
    } catch (e) {}
  }

  function getCultureFromCookie() {
    try {
      const local = localStorage.getItem('user_locale');
      if (local && SUPPORTED_LANGS.includes(local)) return local;
    } catch (e) {}

    const cookieArr = document.cookie.split(';');
    for (let i = 0; i < cookieArr.length; i++) {
      const c = cookieArr[i].trim();
      if (c.startsWith(COOKIE_NAME + '=')) {
        const value = c.substring(COOKIE_NAME.length + 1);
        const parts = value.split('|')[0].split('=');
        if (parts[1] && SUPPORTED_LANGS.includes(parts[1])) {
          return parts[1];
        }
      }
    }
    return 'es';
  }

  // Traducción nativa y limpia del DOM (respetando estilos, clases CSS, iconos y diseño)
  function translateDOM(lang) {
    if (lang === 'es' || !DICTIONARY[lang]) return;
    const dict = DICTIONARY[lang];

    // Función auxiliar para obtener traducción
    function getTranslation(text) {
      if (!text) return null;
      const t = text.trim();
      if (dict[t]) return dict[t];
      // Búsqueda insensible a mayúsculas
      const lower = t.toLowerCase();
      for (const k in dict) {
        if (k.toLowerCase() === lower) return dict[k];
      }
      return null;
    }

    // 1. Traducir nodos de texto (TextNodes) sin alterar elementos HTML ni iconos
    const walker = document.createTreeWalker(
      document.body,
      NodeFilter.SHOW_TEXT,
      {
        acceptNode: function (node) {
          if (!node.nodeValue || !node.nodeValue.trim()) return NodeFilter.FILTER_REJECT;
          const parent = node.parentElement;
          if (!parent) return NodeFilter.FILTER_REJECT;
          const tag = parent.tagName.toLowerCase();
          if (tag === 'script' || tag === 'style' || tag === 'textarea' || parent.classList.contains('material-symbols-outlined')) {
            return NodeFilter.FILTER_REJECT;
          }
          return NodeFilter.FILTER_ACCEPT;
        }
      }
    );

    let node;
    while ((node = walker.nextNode())) {
      const trimmed = node.nodeValue.trim();
      const translated = getTranslation(trimmed);
      if (translated) {
        // Preservar espacios al inicio y al final para no alterar separación de iconos
        const leading = node.nodeValue.match(/^\s*/)[0];
        const trailing = node.nodeValue.match(/\s*$/)[0];
        node.nodeValue = leading + translated + trailing;
      }
    }

    // 2. Traducir botones de tipo input (input[type="submit"], input[type="button"])
    document.querySelectorAll('input[type="submit"], input[type="button"]').forEach(btn => {
      const val = btn.value;
      const trans = getTranslation(val);
      if (trans) {
        btn.value = trans;
      }
    });

    // 3. Traducir placeholders de inputs
    document.querySelectorAll('input[placeholder], textarea[placeholder]').forEach(el => {
      const ph = el.getAttribute('placeholder');
      const trans = getTranslation(ph);
      if (trans) {
        el.setAttribute('placeholder', trans);
      }
    });

    // 4. Traducir atributos title y aria-label
    document.querySelectorAll('[title]').forEach(el => {
      const t = el.getAttribute('title');
      const trans = getTranslation(t);
      if (trans) {
        el.setAttribute('title', trans);
      }
    });
  }

  const currentCulture = getCultureFromCookie();

  function initSelects() {
    const selects = document.querySelectorAll('.locale-select');
    selects.forEach(function (select) {
      select.value = currentCulture;

      if (select.dataset.localeBound) return;
      select.dataset.localeBound = "true";

      select.addEventListener('change', function () {
        const culture = this.value;
        setCultureCookie(culture);
        location.reload();
      });
    });

    if (currentCulture !== 'es') {
      translateDOM(currentCulture);
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initSelects);
  } else {
    initSelects();
  }
})();



