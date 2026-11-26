package com.example.myapplication.data.remote

import com.example.myapplication.data.models.Candidate
import com.example.myapplication.data.models.Proposal
import com.example.myapplication.data.models.UserData
import com.example.myapplication.data.models.VotingItem
import com.google.gson.annotations.SerializedName

data class MobileLoginRequest(
    @SerializedName("document") val document: String,
    @SerializedName("password") val password: String
)

data class MobileLoginResponse(
    @SerializedName("token") val token: String,
    @SerializedName("expiresAt") val expiresAt: String,
    @SerializedName("user") val user: MobileUserDto
)

data class MobileUserDto(
    @SerializedName("id") val id: Long = 0,
    @SerializedName("documento") val documento: String = "",
    @SerializedName("nombre") val nombre: String = "",
    @SerializedName("grado") val grado: String = "",
    @SerializedName("rol") val rol: String = "",
    @SerializedName("status") val status: String = "ACTIVO"
) {
    fun toDomain(): UserData = UserData(
        documento = documento,
        nombre = nombre,
        grado = grado,
        rol = rol,
        status = status
    )
}

data class MobileProposalDto(
    @SerializedName("id") val id: String = "",
    @SerializedName("text") val text: String = ""
) {
    fun toDomain(): Proposal = Proposal(id = id, text = text)
}

data class MobileCandidateDto(
    @SerializedName("id") val id: String = "",
    @SerializedName("name") val name: String = "",
    @SerializedName("number") val number: String = "00",
    @SerializedName("grade") val grade: String = "General",
    @SerializedName("slogan") val slogan: String = "",
    @SerializedName("summary") val summary: String = "",
    @SerializedName("proposals") val proposals: List<MobileProposalDto> = emptyList(),
    @SerializedName("photoUrl") val photoUrl: String? = null,
    @SerializedName("pdfUrl") val pdfUrl: String? = null,
    @SerializedName("isWhiteVote") val isWhiteVote: Boolean = false,
    @SerializedName("status") val status: String = "APROBADO",
    @SerializedName("votes") val votes: Int = 0,
    @SerializedName("percentage") val percentage: Float = 0f
) {
    fun toDomain(): Candidate = Candidate(
        id = id,
        name = name,
        number = number,
        grade = grade,
        slogan = slogan,
        summary = summary,
        proposals = proposals.map { it.toDomain() },
        photoUrl = photoUrl,
        pdfUrl = pdfUrl,
        isWhiteVote = isWhiteVote,
        status = status,
        votes = votes,
        percentage = percentage
    )
}

data class MobileVotingEventDto(
    @SerializedName("id") val id: String = "",
    @SerializedName("title") val title: String = "",
    @SerializedName("description") val description: String? = null,
    @SerializedName("electionType") val electionType: String = "PERSONAS",
    @SerializedName("status") val status: String = "PROGRAMADA",
    @SerializedName("registrationStartDate") val registrationStartDate: Long = 0L,
    @SerializedName("registrationEndDate") val registrationEndDate: Long = 0L,
    @SerializedName("proposalsStartDate") val proposalsStartDate: Long = 0L,
    @SerializedName("proposalsEndDate") val proposalsEndDate: Long = 0L,
    @SerializedName("votingStartDate") val votingStartDate: Long = 0L,
    @SerializedName("votingEndDate") val votingEndDate: Long = 0L,
    @SerializedName("hasVoted") val hasVoted: Boolean = false,
    @SerializedName("candidates") val candidates: List<MobileCandidateDto> = emptyList()
) {
    fun toDomain(): VotingItem = VotingItem(
        id = id,
        title = title,
        description = description,
        electionType = electionType,
        registrationStartDate = registrationStartDate,
        registrationEndDate = registrationEndDate,
        proposalsStartDate = proposalsStartDate,
        proposalsEndDate = proposalsEndDate,
        votingStartDate = votingStartDate,
        votingEndDate = votingEndDate,
        candidates = candidates.map { it.toDomain() },
        hasVoted = hasVoted,
        serverStatus = status
    )
}

data class MobileVoteRequest(
    @SerializedName("eventId") val eventId: Int,
    @SerializedName("candidateId") val candidateId: Int
)

data class MobileVoteResponse(
    @SerializedName("success") val success: Boolean = false,
    @SerializedName("message") val message: String = ""
)

data class MobileErrorResponse(
    @SerializedName("message") val message: String? = null,
    @SerializedName("code") val code: String? = null
)
