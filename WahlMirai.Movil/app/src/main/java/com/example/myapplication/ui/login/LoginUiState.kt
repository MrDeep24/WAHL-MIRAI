package com.example.myapplication.ui.login

data class LoginUiState(
    val documento: String = "",
    val contrasena: String = "",
    val isPasswordVisible: Boolean = false,
    val isLoading: Boolean = false,
    val error: String? = null
)
