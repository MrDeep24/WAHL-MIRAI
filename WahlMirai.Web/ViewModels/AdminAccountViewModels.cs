using System.ComponentModel.DataAnnotations;

namespace WahlMirai.Web.ViewModels;

public class AdminAccountFormViewModel
{
    [Required, StringLength(50)]
    public string Document { get; set; } = string.Empty;

    [Required, MinLength(3), StringLength(150)]
    [RegularExpression(@"^[\p{L}]+(?:[ '\-][\p{L}]+)*$", ErrorMessage = "El nombre solo puede contener letras, espacios, guiones o apóstrofes.")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required, RegularExpression("^(ADMIN|SUPER_ADMIN)$")]
    public string RoleName { get; set; } = "ADMIN";

    [StringLength(100)]
    public string? PositionTitle { get; set; }
}

public class AdminAccountEditViewModel : AdminAccountFormViewModel
{
    [Required]
    public int Id { get; set; }
}