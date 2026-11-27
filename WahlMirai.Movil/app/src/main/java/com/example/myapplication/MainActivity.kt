package com.example.myapplication

import android.os.Bundle
import android.view.WindowManager
import android.widget.Toast
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.material3.Surface
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.NavType
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import androidx.navigation.navArgument
import com.example.myapplication.data.local.VotingDataStore
import com.example.myapplication.data.models.SessionEvent
import com.example.myapplication.data.repository.SessionManager
import com.example.myapplication.data.repository.UserRepositoryImpl
import com.example.myapplication.data.repository.VotingRepository
import kotlinx.coroutines.launch
import com.example.myapplication.ui.components.VotingPatternBackground
import com.example.myapplication.ui.login.LoginScreen
import com.example.myapplication.ui.login.LoginViewModel
import com.example.myapplication.ui.perfil.PerfilScreen
import com.example.myapplication.ui.perfil.PerfilViewModel
import com.example.myapplication.ui.plan.PlanGobiernoScreen
import com.example.myapplication.ui.plan.PlanGobiernoViewModel
import com.example.myapplication.ui.splash.SplashScreen
import com.example.myapplication.ui.theme.MyApplicationTheme
import com.example.myapplication.ui.theme.WahlTheme
import com.example.myapplication.ui.votaciones.VotacionDetalleScreen
import com.example.myapplication.ui.votaciones.VotacionDetalleViewModel
import com.example.myapplication.ui.votaciones.VotacionesScreen
import com.example.myapplication.ui.votaciones.VotacionesViewModel

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // Prohibir capturas y grabación de pantalla en toda la aplicación
        window.setFlags(
            WindowManager.LayoutParams.FLAG_SECURE,
            WindowManager.LayoutParams.FLAG_SECURE
        )
        enableEdgeToEdge()
        setContent {
            MainApp()
        }
    }
}

@Composable
fun MainApp() {
    val context = LocalContext.current
    val scope = rememberCoroutineScope()
    val dataStore = remember { VotingDataStore(context) }
    val userRepository = remember { UserRepositoryImpl() }
    val repository = remember { VotingRepository(context) }

    val isDarkTheme by dataStore.isDarkTheme.collectAsState(initial = false)
    val toggleTheme: () -> Unit = {
        scope.launch { dataStore.setDarkTheme(!isDarkTheme) }
    }
    val navController = rememberNavController()

    // ViewModels centralizados
    val loginViewModel = remember { LoginViewModel(userRepository) }
    val perfilViewModel = remember { PerfilViewModel(userRepository, repository) }
    val votacionesViewModel = remember { VotacionesViewModel(repository) }
    val detalleViewModel = remember { VotacionDetalleViewModel(repository) }
    val planViewModel = remember { PlanGobiernoViewModel(repository) }

    // Listener para eventos de expiración de sesión
    val sessionState by SessionManager.sessionState.collectAsState()
    LaunchedEffect(sessionState) {
        when (sessionState) {
            is SessionEvent.TokenExpirado -> {
                Toast.makeText(
                    context,
                    "Su sesión ha expirado. Por favor inicie sesión de nuevo.",
                    Toast.LENGTH_LONG
                ).show()
                repository.clearCache()
                loginViewModel.clearFields()
                SessionManager.reset()
                navController.navigate("login") {
                    popUpTo(0) { inclusive = true }
                }
            }
            else -> {}
        }
    }

    MyApplicationTheme(darkTheme = isDarkTheme) {
        Surface(
            modifier = Modifier.fillMaxSize(),
            color = WahlTheme.colors.background
        ) {
            Box(modifier = Modifier.fillMaxSize()) {
                VotingPatternBackground()
                NavHost(
                    navController = navController,
                    startDestination = "splash",
                    enterTransition = { fadeIn() },
                    exitTransition = { fadeOut() }
                ) {
                // 1. Splash Screen
                composable("splash") {
                    SplashScreen(
                        onSplashFinished = {
                            navController.navigate("login") {
                                popUpTo("splash") { inclusive = true }
                            }
                        }
                    )
                }

                // 2. Login Screen
                composable("login") {
                    LoginScreen(
                        viewModel = loginViewModel,
                        isDarkTheme = isDarkTheme,
                        onToggleTheme = toggleTheme,
                        onLoginSuccess = { user ->
                            perfilViewModel.setUsuario(user)
                            navController.navigate("votaciones") {
                                popUpTo("login") { inclusive = true }
                            }
                        }
                    )
                }

                // 3. Lista de Votaciones
                composable("votaciones") {
                    VotacionesScreen(
                        viewModel = votacionesViewModel,
                        isDarkTheme = isDarkTheme,
                        onToggleTheme = toggleTheme,
                        onPerfilClick = { navController.navigate("perfil") },
                        onNavigateToDetalle = { votacionId ->
                            navController.navigate("votacion/$votacionId")
                        }
                    )
                }

                // 4. Detalle de Votación (votacion/{id}?candidateId={candidateId})
                composable(
                    route = "votacion/{id}?candidateId={candidateId}",
                    arguments = listOf(
                        navArgument("id") { type = NavType.StringType },
                        navArgument("candidateId") {
                            type = NavType.StringType
                            nullable = true
                            defaultValue = null
                        }
                    )
                ) { backStackEntry ->
                    val votacionId = backStackEntry.arguments?.getString("id") ?: ""
                    val preselectedCandidateId = backStackEntry.arguments?.getString("candidateId")

                    VotacionDetalleScreen(
                        viewModel = detalleViewModel,
                        votacionId = votacionId,
                        preselectedCandidateId = preselectedCandidateId,
                        isDarkTheme = isDarkTheme,
                        onBack = { navController.popBackStack() },
                        onNavigateToPlan = { vId, cId ->
                            navController.navigate("plan/$vId/$cId")
                        },
                        onVoteSuccess = {
                            votacionesViewModel.cargarVotaciones()
                            navController.navigate("votaciones") {
                                popUpTo("votaciones") { inclusive = true }
                            }
                        }
                    )
                }

                // 5. Plan de Gobierno (plan/{votacionId}/{opcionId})
                composable(
                    route = "plan/{votacionId}/{opcionId}",
                    arguments = listOf(
                        navArgument("votacionId") { type = NavType.StringType },
                        navArgument("opcionId") { type = NavType.StringType }
                    )
                ) { backStackEntry ->
                    val votacionId = backStackEntry.arguments?.getString("votacionId") ?: ""
                    val candidateId = backStackEntry.arguments?.getString("opcionId") ?: ""

                    PlanGobiernoScreen(
                        viewModel = planViewModel,
                        votacionId = votacionId,
                        candidateId = candidateId,
                        isDarkTheme = isDarkTheme,
                        onBack = { navController.popBackStack() },
                        onVotarPorCandidato = { vId, cId ->
                            navController.navigate("votacion/$vId?candidateId=$cId") {
                                popUpTo("votacion/$vId") { inclusive = true }
                            }
                        }
                    )
                }

                // 6. Perfil de Usuario
                composable("perfil") {
                    PerfilScreen(
                        viewModel = perfilViewModel,
                        isDarkTheme = isDarkTheme,
                        onToggleTheme = toggleTheme,
                        onBack = { navController.popBackStack() },
                        onCerrarSesion = {
                            loginViewModel.clearFields()
                            navController.navigate("login") {
                                popUpTo(0) { inclusive = true }
                            }
                        }
                    )
                }
            }
            }
        }
    }
}
