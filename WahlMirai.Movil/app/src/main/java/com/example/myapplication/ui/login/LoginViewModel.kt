package com.example.myapplication.ui.login

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

class LoginViewModel(
    private val userRepository: UserRepository = UserRepositoryImpl()
) : ViewModel() {
    private val _uiState = MutableStateFlow(LoginUiState())
    val uiState: StateFlow<LoginUiState> = _uiState.asStateFlow()

    fun onDocumentoChanged(documento: String) {
        // Solo permite números
        if (documento.all { it.isDigit() }) {
            _uiState.update { it.copy(documento = documento, error = null) }
        }
    }

    fun onContrasenaChanged(contrasena: String) {
        _uiState.update { it.copy(contrasena = contrasena, error = null) }
    }

    fun togglePasswordVisibility() {
        _uiState.update { it.copy(isPasswordVisible = !it.isPasswordVisible) }
    }

    fun login(onSuccess: (UserData) -> Unit) {
        if (_uiState.value.isLoading) return

        val doc = _uiState.value.documento.trim()
        val pass = _uiState.value.contrasena

        if (doc.isEmpty() || pass.isBlank()) {
            _uiState.update { it.copy(error = "Por favor ingrese todos los campos") }
            return
        }

        _uiState.update { it.copy(isLoading = true, error = null) }

        viewModelScope.launch {
            // Verificación del usuario contra la base de datos
            val result = userRepository.verifyAndAuthenticateUser(doc, pass)
            result.onSuccess { user ->
                _uiState.update { it.copy(isLoading = false, error = null) }
                onSuccess(user)
            }.onFailure { exception ->
                _uiState.update { it.copy(isLoading = false, error = exception.message ?: "Error al verificar usuario") }
            }
        }
    }

    fun clearFields() {
        _uiState.update { it.copy(documento = "", contrasena = "", error = null) }
    }
}
