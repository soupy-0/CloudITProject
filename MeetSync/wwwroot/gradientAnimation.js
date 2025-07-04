
let angle = 135;
let direction = 1;

function animateGradient() {
    angle += direction * 0.2;
    if (angle >= 315 || angle <= 135) direction *= -1;
    document.body.style.background = `linear-gradient(${angle}deg, #4C3BCF, #85e2ff)`;
    requestAnimationFrame(animateGradient);
}

animateGradient();