using System;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace GhostArena
{
    public sealed class CharactersFactory
    {
        private readonly ControllersFactory _controllersFactory;
        private readonly ControllersUpdateService _controllers;
        private readonly GameObject _playerPrefab;
        private readonly GameObject _enemyPrefab;
        private readonly GameObject _projectilePrefab;
        private readonly Transform _runtimeRoot;
        private readonly Camera _gameplayCamera;
        private readonly Vector2 _arenaHalfExtents;

        public CharactersFactory(
            ControllersFactory controllersFactory,
            ControllersUpdateService controllers,
            GameObject playerPrefab,
            GameObject enemyPrefab,
            GameObject projectilePrefab,
            Transform runtimeRoot,
            Camera gameplayCamera,
            Vector2 arenaHalfExtents)
        {
            _controllersFactory = controllersFactory
                ?? throw new ArgumentNullException(nameof(controllersFactory));
            _controllers = controllers ?? throw new ArgumentNullException(nameof(controllers));
            _playerPrefab = playerPrefab != null
                ? playerPrefab
                : throw new ArgumentNullException(nameof(playerPrefab));
            _enemyPrefab = enemyPrefab != null
                ? enemyPrefab
                : throw new ArgumentNullException(nameof(enemyPrefab));
            _projectilePrefab = projectilePrefab != null
                ? projectilePrefab
                : throw new ArgumentNullException(nameof(projectilePrefab));
            _runtimeRoot = runtimeRoot != null
                ? runtimeRoot
                : throw new ArgumentNullException(nameof(runtimeRoot));
            _gameplayCamera = gameplayCamera != null
                ? gameplayCamera
                : throw new ArgumentNullException(nameof(gameplayCamera));
            _arenaHalfExtents = arenaHalfExtents;
        }

        public Character CreatePlayer(
            Transform spawnPoint,
            GameplaySettings settings,
            GhostVisualSettings visualSettings)
        {
            if (spawnPoint == null)
            {
                throw new ArgumentNullException(nameof(spawnPoint));
            }

            GameObject characterObject = Object.Instantiate(
                _playerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                _runtimeRoot);

            try
            {
                Character character = RequireComponent<Character>(characterObject, "Player");
                Rigidbody body = RequireComponent<Rigidbody>(characterObject, "Player");
                WeaponMount weaponMount = RequireComponent<WeaponMount>(characterObject, "Player");
                GhostVisual visual = RequireChildComponent<GhostVisual>(characterObject, "Player");
                weaponMount.Validate();
                ConfigurePlayerBody(body);
                RigidbodyDirectionalMover mover = new RigidbodyDirectionalMover(
                    body,
                    settings.PlayerMovementSpeed);
                RigidbodyDirectionalRotator rotator = new RigidbodyDirectionalRotator(body);
                character.Initialize(
                    CharacterRole.Player,
                    new Health(settings.PlayerMaximumHealth),
                    mover,
                    null,
                    rotator);
                Weapon weapon = new Weapon(
                    character,
                    weaponMount.Muzzle,
                    _projectilePrefab,
                    _runtimeRoot,
                    settings.Projectile,
                    new CharacterRoleDamagePolicy(CharacterRole.Enemy));
                character.BindWeapon(weapon);
                Controller controller = _controllersFactory.CreatePlayer(character, _gameplayCamera);
                character.BindController(controller);
                _controllers.Add(controller);
                visual.Initialize(visualSettings);
                return character;
            }
            catch
            {
                Object.Destroy(characterObject);
                throw;
            }
        }

        public Character CreateEnemy(
            Transform spawnPoint,
            EnemySettings settings,
            GhostVisualSettings visualSettings)
        {
            if (spawnPoint == null)
            {
                throw new ArgumentNullException(nameof(spawnPoint));
            }

            GameObject characterObject = Object.Instantiate(
                _enemyPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                _runtimeRoot);

            try
            {
                Character character = RequireComponent<Character>(characterObject, "Enemy");
                Rigidbody body = RequireComponent<Rigidbody>(characterObject, "Enemy");
                NavMeshAgent agent = RequireComponent<NavMeshAgent>(characterObject, "Enemy");
                ContactDamage contactDamage = RequireComponent<ContactDamage>(characterObject, "Enemy");
                GhostVisual visual = RequireChildComponent<GhostVisual>(characterObject, "Enemy");

                if (agent.isOnNavMesh == false)
                {
                    throw new InvalidOperationException("Enemy navigation agent is not placed on a NavMesh.");
                }

                ConfigureEnemyBody(body);
                float rotationSpeed = agent.angularSpeed;
                AgentMover mover = new AgentMover(agent, settings.MovementSpeed);
                TransformDirectionalRotator rotator =
                    new TransformDirectionalRotator(character.transform, rotationSpeed);
                character.Initialize(
                    CharacterRole.Enemy,
                    new Health(settings.MaximumHealth),
                    null,
                    mover,
                    rotator);
                Controller controller = _controllersFactory.CreateEnemy(
                    character,
                    _arenaHalfExtents,
                    settings.DirectionInterval,
                    settings.MovementSpeed * settings.DirectionInterval);
                character.BindController(controller);
                _controllers.Add(controller);
                contactDamage.Initialize(
                    character,
                    settings.ContactDamage,
                    new CharacterRoleDamagePolicy(CharacterRole.Player));
                visual.Initialize(visualSettings);
                return character;
            }
            catch
            {
                Object.Destroy(characterObject);
                throw;
            }
        }

        private static T RequireComponent<T>(GameObject characterObject, string characterName)
            where T : Component
        {
            T component = characterObject.GetComponent<T>();

            if (component == null)
            {
                throw new InvalidOperationException(
                    characterName + " prefab has no " + typeof(T).Name + " component.");
            }

            return component;
        }

        private static T RequireChildComponent<T>(GameObject characterObject, string characterName)
            where T : Component
        {
            T component = characterObject.GetComponentInChildren<T>(true);

            if (component == null)
            {
                throw new InvalidOperationException(
                    characterName + " prefab has no " + typeof(T).Name + " component.");
            }

            return component;
        }

        private static void ConfigurePlayerBody(Rigidbody body)
        {
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezePositionY
                | RigidbodyConstraints.FreezeRotationX
                | RigidbodyConstraints.FreezeRotationZ;
        }

        private static void ConfigureEnemyBody(Rigidbody body)
        {
            body.useGravity = false;
            body.isKinematic = true;
            body.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        }
    }
}
