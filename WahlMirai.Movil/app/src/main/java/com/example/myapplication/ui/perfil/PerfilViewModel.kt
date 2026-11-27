package com.example.myapplication.ui.perfil

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.myapplication.data.models.UserData
import com.example.myapplication.data.repository.UserRepository
import com.example.myapplication.data.repository.UserRepositoryImpl
import com.example.myapplication.data.repository.VotingRepository
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

class PerfilViewModel(
    private val userRepository: UserRepository = UserRepositoryImpl(),
    private val votingRepository: VotingRepository? = null
) : ViewModel() {
    private val _uiState = MutableStateFlow(PerfilUiState())
    val uiState: StateFlow<PerfilUiState> = _uiState.asStateFlow()
    private var userJob: Job? = null

    fun setUsuario(userData: UserData) {
        _uiState.update { it.copy(user = userData) }
        userJob?.cancel()
        userJob = viewModelScope.launch {
            userRepository.getUserByDocumentFlow(userData.documento).collect { updatedUser ->
                if (updatedUser != null) {
                    _uiState.update { it.copy(user = updatedUser) }
                }
            }
        }
    }

    fun cerrarSesion() {
        userJob?.cancel()
        votingRepository?.clearCache()
        viewModelScope.launch {
            userRepository.logout()
        }
        _uiState.update { PerfilUiState() }
    }
}
