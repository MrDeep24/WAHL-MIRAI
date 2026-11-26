package com.example.myapplication.ui.perfil

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.example.myapplication.data.models.UserData
import com.example.myapplication.data.repository.UserRepository
import com.example.myapplication.data.repository.UserRepositoryImpl
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch

class PerfilViewModel(
    private val userRepository: UserRepository = UserRepositoryImpl()
) : ViewModel() {
    private val _uiState = MutableStateFlow(PerfilUiState())
    val uiState: StateFlow<PerfilUiState> = _uiState.asStateFlow()

    fun setUsuario(userData: UserData) {
        _uiState.update { it.copy(user = userData) }
        viewModelScope.launch {
            userRepository.getUserByDocumentFlow(userData.documento).collect { updatedUser ->
                if (updatedUser != null) {
                    _uiState.update { it.copy(user = updatedUser) }
                }
            }
        }
    }

    fun cerrarSesion() {
        viewModelScope.launch {
            userRepository.logout()
        }
        _uiState.update { PerfilUiState() }
    }
}
