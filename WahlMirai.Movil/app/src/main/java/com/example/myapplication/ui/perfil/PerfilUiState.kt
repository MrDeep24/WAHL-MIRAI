package com.example.myapplication.ui.perfil

import com.example.myapplication.data.models.UserData

data class PerfilUiState(
    val isLoading: Boolean = false,
    val user: UserData = UserData()
)
