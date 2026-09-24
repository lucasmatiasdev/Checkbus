using MudBlazor;

namespace Checkbus.Web.Components.Landing;

/// <summary>
/// A single item in the "Soluciones" section: an icon, a title, and a short description.
/// </summary>
public sealed record SolutionItem(string Icon, string Title, string Description);

/// <summary>
/// A single pricing tier in the "Precios" section.
/// </summary>
public sealed record PricingPlan(
    string Name,
    string Price,
    string Period,
    string Description,
    IReadOnlyList<string> Features,
    bool Highlighted,
    string CtaLabel);

/// <summary>
/// A single customer testimonial in the "Testimonios" section.
/// </summary>
public sealed record Testimonial(string Quote, string Author, string Role, string Company, string Initials);

/// <summary>
/// Static placeholder content for the landing page sections. Content lives here (not in
/// <c>Home.razor</c>'s <c>@code</c> block) so a future API-backed swap replaces this class
/// without touching card markup, and "exactly 3 plans" becomes a unit-testable assertion.
/// </summary>
public static class LandingContent
{
    public static readonly IReadOnlyList<SolutionItem> Solutions =
    [
        new(Icons.Material.Filled.Route, "Planificación de rutas",
            "Optimiza recorridos y horarios de tu flota en minutos."),
        new(Icons.Material.Filled.Groups, "Gestión de pasajeros",
            "Controla reservas, boletos y ocupación en tiempo real."),
        new(Icons.Material.Filled.Analytics, "Reportes en vivo",
            "Visualiza métricas de ocupación, ingresos y puntualidad al instante."),
        new(Icons.Material.Filled.SupportAgent, "Soporte dedicado",
            "Un equipo especializado te acompaña durante toda la operación."),
    ];

    /// <summary>Exactly 3 static placeholder SaaS tiers — enforced by <c>LandingContentTests</c>.</summary>
    public static readonly IReadOnlyList<PricingPlan> Plans =
    [
        new(
            Name: "Starter",
            Price: "$29",
            Period: "/mes",
            Description: "Para operadores pequeños que están comenzando.",
            Features: ["Hasta 5 unidades", "Soporte por correo", "Reportes básicos"],
            Highlighted: false,
            CtaLabel: "Comenzar"),
        new(
            Name: "Pro",
            Price: "$79",
            Period: "/mes",
            Description: "Para flotas en crecimiento que necesitan más control.",
            Features: ["Hasta 25 unidades", "Soporte prioritario", "Reportes avanzados", "Integraciones API"],
            Highlighted: true,
            CtaLabel: "Elegir Pro"),
        new(
            Name: "Enterprise",
            Price: "A medida",
            Period: "",
            Description: "Para operadores grandes con necesidades a medida.",
            Features: ["Unidades ilimitadas", "Soporte dedicado 24/7", "SLA personalizado", "Integraciones a medida"],
            Highlighted: false,
            CtaLabel: "Contactar ventas"),
    ];

    public static readonly IReadOnlyList<Testimonial> Testimonials =
    [
        new(
            Quote: "Checkbus nos ayudó a reducir el tiempo de planificación de rutas en un 40%.",
            Author: "María Fernández",
            Role: "Directora de Operaciones",
            Company: "Transportes Rápidos",
            Initials: "MF"),
        new(
            Quote: "La visibilidad en tiempo real cambió por completo cómo gestionamos la flota.",
            Author: "Carlos Ibarra",
            Role: "Gerente de Flota",
            Company: "AutoBus Norte",
            Initials: "CI"),
        new(
            Quote: "El soporte es excelente y la plataforma es muy fácil de usar para todo el equipo.",
            Author: "Lucía Gómez",
            Role: "Jefa de Atención al Cliente",
            Company: "Rutas del Sur",
            Initials: "LG"),
    ];

    /// <summary>
    /// Single source of truth for the contact email, referenced identically by both the
    /// Contacto and Footer sections so their contact info can never drift apart.
    /// </summary>
    public static readonly string ContactEmail = "contacto@checkbus.com";
}
