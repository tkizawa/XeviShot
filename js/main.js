window.addEventListener('load', function() {
    const canvas = document.getElementById('gameCanvas');
    const ctx = canvas.getContext('2d');
    
    // Set actual canvas size
    canvas.width = 480;
    canvas.height = 640;

    let game;
    let animationId;
    let currentState = 'TITLE'; // TITLE, PLAYING, GAMEOVER

    const titleScreen = document.getElementById('title-screen');
    const gameOverScreen = document.getElementById('game-over-screen');
    const finalScoreEl = document.getElementById('final-score');

    // Input handling
    const keys = {};
    window.addEventListener('keydown', e => {
        keys[e.key] = true;
        
        // Handle State Transitions
        if (currentState === 'TITLE') {
            startGame();
        } else if (currentState === 'GAMEOVER') {
            startGame();
        }
    });
    
    window.addEventListener('keyup', e => {
        keys[e.key] = false;
    });

    function startGame() {
        game = new Game(canvas.width, canvas.height);
        game.keys = keys; // Pass reference
        
        currentState = 'PLAYING';
        titleScreen.classList.add('hidden');
        gameOverScreen.classList.add('hidden');
        
        document.getElementById('score').innerText = '0';
        document.getElementById('lives').innerText = '3';
        
        if (animationId) cancelAnimationFrame(animationId);
        animate();
    }

    function animate() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        
        if (currentState === 'PLAYING') {
            game.update();
            game.draw(ctx);
            
            if (game.gameOver) {
                currentState = 'GAMEOVER';
                gameOverScreen.classList.remove('hidden');
                finalScoreEl.innerText = game.score;
            } else {
                animationId = requestAnimationFrame(animate);
            }
        }
    }
});
