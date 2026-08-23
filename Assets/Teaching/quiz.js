document.querySelectorAll('[data-quiz]').forEach((quiz) => {
  const feedback = quiz.querySelector('[data-feedback]');
  quiz.querySelectorAll('[data-answer]').forEach((button) => {
    button.addEventListener('click', () => {
      const correct = button.dataset.answer === 'correct';
      quiz.querySelectorAll('[data-answer]').forEach((candidate) => {
        candidate.classList.remove('correct', 'incorrect');
        candidate.removeAttribute('aria-current');
      });
      button.classList.add(correct ? 'correct' : 'incorrect');
      button.setAttribute('aria-current', 'true');
      feedback.textContent = correct
        ? quiz.dataset.correct
        : quiz.dataset.incorrect;
    });
  });
});
