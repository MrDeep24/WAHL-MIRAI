package com.example.myapplication.data.remote

import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.POST
import retrofit2.http.Path

interface WahlMiraiApiService {

    @POST("api/mobile/login")
    suspend fun login(@Body request: MobileLoginRequest): Response<MobileLoginResponse>

    @POST("api/mobile/logout")
    suspend fun logout(): Response<Unit>

    @GET("api/mobile/profile")
    suspend fun getProfile(): Response<MobileUserDto>

    @GET("api/mobile/events")
    suspend fun getEvents(): Response<List<MobileVotingEventDto>>

    @GET("api/mobile/events/{id}")
    suspend fun getEventById(@Path("id") id: String): Response<MobileVotingEventDto>

    @POST("api/mobile/vote")
    suspend fun castVote(@Body request: MobileVoteRequest): Response<MobileVoteResponse>
}
