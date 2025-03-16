async function logMovies() {
    const response = await fetch("https://localhost:7188/api/DoctorApi");
    const data = await response.json();
    console.log(data)
    
    document.querySelector("#getL").innerHTML = data[1].name ;
}
logMovies();