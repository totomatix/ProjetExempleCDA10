// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

const delay = ms => new Promise(res => setTimeout(res, ms));
// Write your JavaScript code.
function DemanderConfirmation(event) {
    let rep = confirm("Voulez vous vraiment supprimer ce livre ?");
    if (rep != true) {
        event.preventDefault();
    }
}

let formsSupprLivre = document.getElementsByClassName("formSupprLivre");
Array.from(formsSupprLivre).forEach(form => {
    form.addEventListener("submit", DemanderConfirmation)
});

async function rechercheLivre() {
    recherche = document.getElementById("recherche").value;
    const reponse = await fetch("http://localhost:5293/Livres/rechercheLivre?nom=" + recherche);
    const livres = await reponse.json();
    document.getElementById("affichageRecherche").innerHTML = "";
    livres.forEach(l => AfficherLivre(l));
}

function AfficherLivre(livre) {
    let l = document.createElement("div");
    let titre = document.createElement("a");
    titre.href = "http://localhost:5293/Livres/Detail/" + livre.id;
    titre.textContent = livre.titre;
    l.appendChild(titre);
    document.getElementById("affichageRecherche").appendChild(l);
}

let barreRecherche = document.getElementById("recherche");
barreRecherche.addEventListener("input", rechercheLivre);
async function toggleRecherche() {
    let divRecherche = document.getElementById("affichageRecherche");
    if (divRecherche.classList.contains("actif")) {
        await delay(500);
    }
    divRecherche.classList.toggle("actif");
}

barreRecherche.addEventListener("focus", toggleRecherche);
barreRecherche.addEventListener("focusout", toggleRecherche);