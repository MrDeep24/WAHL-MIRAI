package com.example.myapplication.ui.login

import androidx.compose.foundation.BorderStroke
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Badge
import androidx.compose.material.icons.filled.ErrorOutline
import androidx.compose.material.icons.filled.Lock
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.example.myapplication.R
import com.example.myapplication.data.models.UserData
import com.example.myapplication.ui.components.ThemeToggleSwitch
import com.example.myapplication.ui.theme.WahlTheme

@Composable
fun LoginScreen(
    viewModel: LoginViewModel = viewModel(),
    isDarkTheme: Boolean = true,
    onToggleTheme: () -> Unit = {},
    onLoginSuccess: (UserData) -> Unit
) {
    val uiState by viewModel.uiState.collectAsState()
    val colors = WahlTheme.colors
    val hasError = uiState.error != null

    val scrollState = rememberScrollState()

    Box(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.Transparent)
            .systemBarsPadding()
    ) {
        Column(
            modifier = Modifier
                .fillMaxSize()
                .verticalScroll(scrollState)
                .padding(horizontal = 24.dp, vertical = 20.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(modifier = Modifier.weight(0.7f))

            // Header: Logo Oficial y Títulos
            Column(horizontalAlignment = Alignment.CenterHorizontally) {
                Surface(
                    shape = RoundedCornerShape(24.dp),
                    color = Color.Transparent,
                    shadowElevation = 8.dp,
                    modifier = Modifier.size(100.dp)
                ) {
                    Image(
                        painter = painterResource(id = R.drawable.gemini_generated_image_c63ls8c63ls8c63l),
                        contentDescription = "Logo WAHL MIRAI",
                        modifier = Modifier.fillMaxSize()
                    )
                }

                Spacer(modifier = Modifier.height(16.dp))

                Text(
                    text = "WAHL MIRAI",
                    style = MaterialTheme.typography.headlineLarge,
                    color = colors.text
                )

                Spacer(modifier = Modifier.height(4.dp))

                Text(
                    text = "Sistema de Votación Estudiantil",
                    style = MaterialTheme.typography.bodyMedium,
                    color = colors.subtext
                )
            }

            Spacer(modifier = Modifier.height(28.dp))

            // Tarjeta contenedora del formulario (estilo institucional)
            Card(
                shape = RoundedCornerShape(20.dp),
                colors = CardDefaults.cardColors(containerColor = colors.card),
                elevation = CardDefaults.cardElevation(defaultElevation = if (colors.isDark) 0.dp else 4.dp),
                border = BorderStroke(1.dp, colors.border),
                modifier = Modifier.fillMaxWidth()
            ) {
                Column(
                    modifier = Modifier
                        .fillMaxWidth()
                        .padding(20.dp),
                    verticalArrangement = Arrangement.spacedBy(16.dp)
                ) {
                    // Campo 1: Número de documento
                    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                        Text(
                            text = "Número de documento",
                            color = colors.text,
                            fontSize = 14.sp,
                            fontWeight = FontWeight.SemiBold
                        )
                        OutlinedTextField(
                            value = uiState.documento,
                            onValueChange = { viewModel.onDocumentoChanged(it) },
                            modifier = Modifier.fillMaxWidth(),
                            shape = RoundedCornerShape(10.dp),
                            textStyle = TextStyle(
                                fontFamily = FontFamily.Monospace,
                                fontSize = 16.sp,
                                fontWeight = FontWeight.Medium
                            ),
                            isError = hasError,
                            leadingIcon = {
                                Icon(
                                    Icons.Default.Badge,
                                    contentDescription = null,
                                    tint = if (hasError) colors.error else colors.subtext
                                )
                            },
                            placeholder = { Text("Ej: 1001234567", color = colors.subtext) },
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedContainerColor = colors.surfaceLow,
                                unfocusedContainerColor = colors.surfaceLow,
                                errorContainerColor = colors.surfaceLow,
                                focusedBorderColor = colors.primary,
                                unfocusedBorderColor = colors.border,
                                errorBorderColor = colors.error,
                                focusedTextColor = colors.text,
                                unfocusedTextColor = colors.text,
                                errorTextColor = colors.text
                            ),
                            singleLine = true,
                            enabled = !uiState.isLoading,
                            keyboardOptions = KeyboardOptions(
                                keyboardType = KeyboardType.Number,
                                imeAction = ImeAction.Next
                            )
                        )
                    }

                    // Campo 2: Contraseña
                    Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
                        Text(
                            text = "Contraseña",
                            color = colors.text,
                            fontSize = 14.sp,
                            fontWeight = FontWeight.SemiBold
                        )
                        OutlinedTextField(
                            value = uiState.contrasena,
                            onValueChange = { viewModel.onContrasenaChanged(it) },
                            modifier = Modifier.fillMaxWidth(),
                            shape = RoundedCornerShape(10.dp),
                            isError = hasError,
                            leadingIcon = {
                                Icon(
                                    Icons.Default.Lock,
                                    contentDescription = null,
                                    tint = if (hasError) colors.error else colors.subtext
                                )
                            },
                            trailingIcon = {
                                IconButton(
                                    onClick = { viewModel.togglePasswordVisibility() },
                                    enabled = !uiState.isLoading
                                ) {
                                    Icon(
                                        imageVector = if (uiState.isPasswordVisible) Icons.Default.Visibility else Icons.Default.VisibilityOff,
                                        contentDescription = "Mostrar contraseña",
                                        tint = colors.subtext
                                    )
                                }
                            },
                            visualTransformation = if (uiState.isPasswordVisible) VisualTransformation.None else PasswordVisualTransformation(),
                            colors = OutlinedTextFieldDefaults.colors(
                                focusedContainerColor = colors.surfaceLow,
                                unfocusedContainerColor = colors.surfaceLow,
                                errorContainerColor = colors.surfaceLow,
                                focusedBorderColor = colors.primary,
                                unfocusedBorderColor = colors.border,
                                errorBorderColor = colors.error,
                                focusedTextColor = colors.text,
                                unfocusedTextColor = colors.text,
                                errorTextColor = colors.text
                            ),
                            placeholder = { Text("********", color = colors.subtext) },
                            singleLine = true,
                            enabled = !uiState.isLoading,
                            keyboardOptions = KeyboardOptions(
                                keyboardType = KeyboardType.Password,
                                imeAction = ImeAction.Done
                            ),
                            keyboardActions = KeyboardActions(
                                onDone = { viewModel.login(onSuccess = onLoginSuccess) }
                            )
                        )
                    }

                    if (uiState.error != null) {
                        Row(
                            modifier = Modifier
                                .fillMaxWidth()
                                .background(colors.errorBg, RoundedCornerShape(10.dp))
                                .padding(horizontal = 12.dp, vertical = 10.dp),
                            verticalAlignment = Alignment.CenterVertically,
                            horizontalArrangement = Arrangement.spacedBy(8.dp)
                        ) {
                            Icon(
                                imageVector = Icons.Default.ErrorOutline,
                                contentDescription = null,
                                tint = colors.error,
                                modifier = Modifier.size(18.dp)
                            )
                            Text(
                                text = uiState.error!!,
                                color = colors.error,
                                fontSize = 13.sp,
                                fontWeight = FontWeight.Medium
                            )
                        }
                    }

                    Spacer(modifier = Modifier.height(4.dp))

                    // Botón Acceder al sistema
                    Button(
                        onClick = { viewModel.login(onSuccess = onLoginSuccess) },
                        enabled = !uiState.isLoading,
                        modifier = Modifier
                            .fillMaxWidth()
                            .height(52.dp),
                        shape = RoundedCornerShape(10.dp),
                        colors = ButtonDefaults.buttonColors(
                            containerColor = colors.primaryContainer,
                            disabledContainerColor = colors.primaryContainer.copy(alpha = 0.7f)
                        )
                    ) {
                        if (uiState.isLoading) {
                            CircularProgressIndicator(
                                modifier = Modifier.size(22.dp),
                                color = Color.White,
                                strokeWidth = 2.5.dp
                            )
                        } else {
                            Text(
                                text = "Acceder al sistema",
                                fontSize = 16.sp,
                                fontWeight = FontWeight.Bold,
                                color = Color.White
                            )
                        }
                    }
                }
            }

            Spacer(modifier = Modifier.weight(1f))
            Spacer(modifier = Modifier.height(20.dp))

            // Botón Switch Elegante de Tema (Sol / Luna)
            ThemeToggleSwitch(
                isDarkTheme = isDarkTheme,
                onToggleTheme = onToggleTheme
            )

            Spacer(modifier = Modifier.height(12.dp))
        }
    }
}
